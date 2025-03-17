using System.Net;
using CORSProxy.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CORSProxy
{
    public class ProxyFunction
    {
        private static string[] OriginBlacklist = Array.Empty<string>(); // Example: { "http://bad-origin.com" }
        private static string[] OriginWhitelist = Array.Empty<string>(); // Example: { "http://allowed-origin.com" }
        private static int MaxRedirects = 5;
        private static int CorsMaxAge = 3600;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly IConfiguration _config;

        public ProxyFunction(ILoggerFactory loggerFactory,
            IHttpClientFactory httpClientFactory,
            IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _logger = loggerFactory.CreateLogger<ProxyFunction>();
            _config = config;

            // Fetch settings from configuration
            OriginBlacklist = _config["CORS_BLACKLIST"]?.Split(',') ?? Array.Empty<string>();
            OriginWhitelist = _config["CORS_WHITELIST"]?.Split(',') ?? Array.Empty<string>();
            MaxRedirects = int.TryParse(_config["MAX_REDIRECTS"], out int max) ? max : 5;
            CorsMaxAge = int.TryParse(_config["CORS_MAX_AGE"], out int maxAge) ? maxAge : 3600;
        }

        [Function("CorsProxy")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");


            // Enforce origin restrictions (if configured)
            if (!IsOriginAllowed(req))
            {
                return await req.CreateBadRequestResponse("Origin is not allowed.", HttpStatusCode.Forbidden);
            }

            // Handle Preflight Requests
            if (req.Method == HttpMethods.Options)
            {
                var preflightResponse = req.CreateResponse(HttpStatusCode.OK);
                CorsHelper.ApplyCorsHeaders(preflightResponse, req);
                return preflightResponse;
            }

            // Extract target URL
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var targetUrl = query["url"];
            if (string.IsNullOrEmpty(targetUrl))
                return await req.CreateBadRequestResponse("Please provide a valid 'url' query parameter.");

            // Parse the URI to extract hostname
            Uri? targetUri;
            if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out targetUri))
                return await req.CreateBadRequestResponse("Invalid URL format.");

            // ✅ NEW: Check if the hostname has a valid TLD
            if (!HostValidator.IsValidHost(targetUri.Host))
            {
                _logger.LogInformation($"Validating Host: {targetUri.Host}");

                return await req.CreateBadRequestResponse($"Invalid host: {targetUri.Host}");
            }

            _logger.LogInformation($"Received Validated Proxy Request for: {targetUri}");
            var requestMessage = new HttpRequestMessage(new HttpMethod(req.Method), targetUrl);
            _logger.LogInformation($"Forwarding request to: {requestMessage.RequestUri}");

            // Create request message
            var proxyRequest = new HttpRequestMessage(new HttpMethod(req.Method), targetUrl);

            var ignoredHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Host", "Connection", "Accept-Encoding", "Cache-Control", "postman-token"
            };

            // Only forward safe headers
            foreach (var header in req.Headers)
            {
                if (!ignoredHeaders.Contains(header.Key) && !proxyRequest.Headers.Contains(header.Key))
                {
                    proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.AsEnumerable());
                }
            }
            _logger.LogInformation($"Headers: {string.Join(",", proxyRequest.Headers.Select(x => x.Key))}");

            // Create an HttpClient instance
            var httpClient = _httpClientFactory.CreateClient("IgnoreSSL");

            // Send request to the target server
            HttpResponseMessage proxyResponse;
            try
            {
                proxyResponse = await httpClient.SendAsync(proxyRequest);
                var responseBody = await proxyResponse.Content.ReadAsStringAsync();

                _logger.LogInformation($"Response Status: {proxyResponse.StatusCode}");
                _logger.LogInformation($"Response Body: {responseBody}");
            }
            catch (Exception ex)
            {
                return await req.CreateErrorResponse($"Failed to connect to the target server.\n{ex}", HttpStatusCode.BadGateway);
            }

            // Create response
            var response = await HandleProxyResponse(req, proxyResponse);
            
            return response;
        }

        public async Task<HttpResponseData> HandleProxyResponse(HttpRequestData req, HttpResponseMessage proxyResponse)
        {
            var response = req.CreateResponse(proxyResponse.StatusCode);

            // Add CORS Headers
            CorsHelper.ApplyCorsHeaders(response, req);

            // Extract headers from proxy response
            foreach (var header in proxyResponse.Headers)
            {
                if (!response.Headers.Contains(header.Key)) // Avoid duplicates
                {
                    response.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // ✅ Debugging Headers
            response.Headers.TryAddWithoutValidation("x-request-url", req.Url.ToString());
            response.Headers.TryAddWithoutValidation("x-final-url", req.Url.ToString());

            // ✅ Handle redirects
            if ((int)proxyResponse.StatusCode >= 300 && (int)proxyResponse.StatusCode < 400)
            {
                if (proxyResponse.Headers.Location != null)
                {
                    var newLocation = proxyResponse.Headers.Location.ToString();
                    response.Headers.TryAddWithoutValidation("X-CORS-Redirect", $"{(int)proxyResponse.StatusCode} {newLocation}");

                    // Follow the redirect internally (limited to avoid infinite loops)
                    if (!req.Headers.Contains("X-Redirect-Count"))
                    {
                        req.Headers.TryAddWithoutValidation("X-Redirect-Count", "1");
                    }
                    else if (int.TryParse(req.Headers.GetValues("X-Redirect-Count").FirstOrDefault(), out int redirectCount) && redirectCount < 5)
                    {
                        req.Headers.Remove("X-Redirect-Count");
                        req.Headers.TryAddWithoutValidation("X-Redirect-Count", (redirectCount + 1).ToString());

                        // ✅ Properly follow redirect
                        var newRequest = new HttpRequestMessage(HttpMethod.Get, newLocation);
                        var httpClient = _httpClientFactory.CreateClient("IgnoreSSL");
                        var newResponse = await httpClient.SendAsync(newRequest);
                        return await HandleProxyResponse(req, newResponse);
                    }
                }
            }

            // Strip cookies
            response.Headers.Remove("Set-Cookie");
            response.Headers.Remove("Set-Cookie2");
            response.Headers.Remove("Transfer-Encoding"); // Avoid chunked encoding issues

            // ✅ Copy response content & set Content-Type explicitly
            var content = await proxyResponse.Content.ReadAsStringAsync();
            response.Headers.TryAddWithoutValidation("Content-Type", proxyResponse.Content.Headers.ContentType?.ToString() ?? "application/json");
            await response.WriteStringAsync(content);

            return response;
        }

        public void SetHeaders()
        { 
        
        }

        /// <summary>
        /// Check if the request's origin is allowed.
        /// </summary>
        private static bool IsOriginAllowed(HttpRequestData req)
        {
            if (OriginWhitelist.Length == 0) return true; // Allow all if whitelist is empty

            if (req.Headers.TryGetValues("Origin", out var origins))
            {
                return origins.Any(origin => OriginWhitelist.Contains(origin));
            }

            return false;
        }
    }
}
