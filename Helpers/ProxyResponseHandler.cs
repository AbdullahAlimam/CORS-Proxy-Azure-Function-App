using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace CORSProxy.Helpers
{
    /// <summary>
    /// Handles response modifications for the CORS Proxy, including headers and redirects.
    /// </summary>
    public static class ProxyResponseHandler
    {
        private static int MaxRedirects = 5;

        /// <summary>
        /// Loads configuration settings, including the maximum number of allowed redirects.
        /// </summary>
        /// <param name="config">The configuration provider containing application settings.</param>
        public static void LoadConfiguration(IConfiguration config)
        {
            MaxRedirects = int.TryParse(config["MAX_REDIRECTS"], out int max) ? max : 5;
        }

        /// <summary>
        /// Processes and modifies the proxy response before returning it to the client.
        /// </summary>
        /// <param name="req">The original HTTP request from the client.</param>
        /// <param name="proxyResponse">The response received from the target server.</param>
        /// <returns>A modified <see cref="HttpResponseData"/> object with appropriate CORS headers and security adjustments.</returns>
        public static async Task<HttpResponseData> HandleProxyResponse(HttpRequestData req, HttpResponseMessage proxyResponse)
        {
            var response = req.CreateResponse(proxyResponse.StatusCode);

            // Apply CORS Headers
            CorsHelper.ApplyCorsHeaders(response, req);

            // Copy headers from the proxy response
            foreach (var header in proxyResponse.Headers)
            {
                if (!response.Headers.Contains(header.Key))
                {
                    response.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Handle redirects
            if ((int)proxyResponse.StatusCode >= 300 && (int)proxyResponse.StatusCode < 400 && proxyResponse.Headers.Location != null)
            {
                response.Headers.TryAddWithoutValidation("X-CORS-Redirect", $"{(int)proxyResponse.StatusCode} {proxyResponse.Headers.Location}");

                if(ValidRedirectCount(req))
                    return await FollowRedirect(req, proxyResponse.Headers.Location.ToString());
            }

            // Remove sensitive headers
            response.Headers.Remove("Set-Cookie");
            response.Headers.Remove("Set-Cookie2");
            response.Headers.Remove("Transfer-Encoding");

            // Set Content-Type explicitly
            var content = await proxyResponse.Content.ReadAsStringAsync();
            response.Headers.TryAddWithoutValidation("Content-Type", proxyResponse.Content.Headers.ContentType?.ToString() ?? "application/json");
            await response.WriteStringAsync(content);

            return response;
        }

        /// <summary>
        /// Follows an HTTP redirect and retrieves the response from the new location.
        /// </summary>
        /// <param name="req">The original HTTP request from the client.</param>
        /// <param name="newLocation">The new URL to follow.</param>
        /// <returns>A <see cref="HttpResponseData"/> object containing the redirected response.</returns>
        private static async Task<HttpResponseData> FollowRedirect(HttpRequestData req, string newLocation)
        {
            // ✅ Properly follow redirect
            var httpClient = HttpClientFactoryProvider.Instance.CreateClient("IgnoreSSL");
            var newRequest = new HttpRequestMessage(HttpMethod.Get, newLocation);
            var newResponse = await httpClient.SendAsync(newRequest);
            return await HandleProxyResponse(req, newResponse);
        }

        /// <summary>
        /// Determines whether the request has exceeded the allowed number of redirects.
        /// </summary>
        /// <param name="req">The original HTTP request from the client.</param>
        /// <returns><c>true</c> if the redirect count is within the allowed limit; otherwise, <c>false</c>.</returns>
        private static bool ValidRedirectCount(HttpRequestData req)
        {
            // Follow the redirect internally (limited to avoid infinite loops)
            if (!req.Headers.Contains("X-Redirect-Count"))
            {
                req.Headers.TryAddWithoutValidation("X-Redirect-Count", "1");
                return true;
            }
            else if (int.TryParse(req.Headers.GetValues("X-Redirect-Count").FirstOrDefault(), out int redirectCount) && redirectCount < 5)
            {
                req.Headers.Remove("X-Redirect-Count");
                req.Headers.TryAddWithoutValidation("X-Redirect-Count", (redirectCount + 1).ToString());
                return true;
            }

            return false;
        }
    }
}
