using System.Net;
using CORSProxy.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CORSProxy.Functions
{
    /// <summary>
    /// Represents the main entry point for handling incoming HTTP requests in the CORS proxy.
    /// </summary>
    public class ProxyFunction
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyFunction"/> class.
        /// </summary>
        /// <param name="loggerFactory">The factory used to create a logger instance.</param>
        public ProxyFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ProxyFunction>();
        }

        /// <summary>
        /// Handles incoming HTTP requests and processes them as a CORS proxy.
        /// </summary>
        /// <param name="req">The incoming HTTP request data.</param>
        /// <returns>A <see cref="Task{HttpResponseData}"/> representing the HTTP response.</returns>
        [Function("CorsProxy")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete")] HttpRequestData req)
        {
            _logger.LogInformation("🔹 Received HTTP request: {Method} {Url}", req.Method, req.Url);

            // ✅ Validate Origin: Check if the request is allowed or blocked
            if (!OriginValidator.IsOriginAllowed(req))
            {
                _logger.LogWarning("❌ Request blocked: Origin is NOT in the whitelist. Origin: {Origin}", req.Headers.GetValues("Origin").FirstOrDefault());
                return await req.CreateBadRequestResponse("Origin is not allowed.", HttpStatusCode.Forbidden);
            }

            if (OriginValidator.IsOriginBlocked(req))
            {
                _logger.LogWarning("❌ Request blocked: Origin is in the blacklist. Origin: {Origin}", req.Headers.GetValues("Origin").FirstOrDefault());
                return await req.CreateBadRequestResponse("Origin is explicitly blocked.", HttpStatusCode.Forbidden);
            }

            _logger.LogInformation("✅ Origin validation passed.");

            // ✅ Handle Preflight Requests (CORS OPTIONS request)
            if (req.Method == HttpMethods.Options)
            {
                _logger.LogInformation("🔹 Handling CORS preflight request.");

                var preflightResponse = req.CreateResponse(HttpStatusCode.OK);
                CorsHelper.ApplyCorsHeaders(preflightResponse, req);

                _logger.LogInformation("✅ Preflight response generated with CORS headers.");
                return preflightResponse;
            }

            // ✅ Extract and validate the target URL from query parameters
            _logger.LogInformation("🔹 Extracting target URL from request query: {Query}", req.Url.Query);

            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var targetUrl = query["url"];

            if (string.IsNullOrEmpty(targetUrl))
            {
                _logger.LogWarning("❌ Missing or empty 'url' parameter in the request.");
                return await req.CreateBadRequestResponse("Please provide a valid 'url' query parameter.", HttpStatusCode.BadRequest);
            }

            _logger.LogInformation("✅ Target URL extracted: {TargetUrl}", targetUrl);

            // ✅ Parse the target URL and validate its format
            _logger.LogInformation("🔹 Parsing target URL: {TargetUrl}", targetUrl);

            Uri? targetUri;
            if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out targetUri))
            {
                _logger.LogWarning("❌ Invalid URL format: {TargetUrl}", targetUrl);
                return await req.CreateBadRequestResponse("Invalid URL format.");
            }

            _logger.LogInformation("✅ Target URL successfully parsed. Host: {Host}, Scheme: {Scheme}", targetUri.Host, targetUri.Scheme);


            // ✅ Validate the target hostname (checks both IP and TLD)
            _logger.LogInformation("🔹 Validating hostname: {Host}", targetUri.Host);

            if (!DomainValidator.IsValidDomain(targetUri.Host))
            {
                _logger.LogWarning("❌ Host validation failed: {Host} is not a valid domain or IP.", targetUri.Host);
                return await req.CreateBadRequestResponse($"Invalid host: {targetUri.Host}", HttpStatusCode.BadRequest);
            }

            _logger.LogInformation("✅ Host validation passed: {Host} is a valid domain or IP.", targetUri.Host);

           
            // Send request to the target server
            HttpResponseMessage proxyResponse;
            try
            {
                proxyResponse = await ProxyRequestHandler.SendProxyRequest(req, targetUrl);
            }
            catch (Exception ex)
            {
                return await req.CreateErrorResponse($"Failed to connect to the target server.\n{ex}", HttpStatusCode.BadGateway);
            }

            // Create response
            var response = await ProxyResponseHandler.HandleProxyResponse(req, proxyResponse);

            return response;
        }
    }
}
