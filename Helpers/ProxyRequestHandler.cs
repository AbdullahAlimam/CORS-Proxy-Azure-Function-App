using Microsoft.Azure.Functions.Worker.Http;

namespace CORSProxy.Helpers
{
    /// <summary>
    /// Handles the construction and validation of outgoing proxy requests.
    /// Ensures that only safe headers are forwarded while filtering out unnecessary or restricted headers.
    /// </summary>
    public static class ProxyRequestHandler
    {
        /// <summary>
        /// A set of headers that should not be forwarded in proxy requests.
        /// </summary>
        private static readonly HashSet<string> IgnoredHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Host", "Connection", "Accept-Encoding", "Cache-Control", "postman-token"
        };

        /// <summary>
        /// Creates an outgoing proxy request by filtering and forwarding only safe headers.
        /// </summary>
        /// <param name="req">The incoming HTTP request received by the proxy.</param>
        /// <param name="targetUrl">The destination URL to which the request will be forwarded.</param>
        /// <returns>An <see cref="HttpRequestMessage"/> containing the modified request with forwarded headers.</returns>
        public static HttpRequestMessage CreateProxyRequest(HttpRequestData req, string targetUrl)
        {
            var proxyRequest = new HttpRequestMessage(new HttpMethod(req.Method), targetUrl);

            // Forward only safe headers
            foreach (var header in req.Headers)
            {
                if (!IgnoredHeaders.Contains(header.Key) && !proxyRequest.Headers.Contains(header.Key))
                {
                    proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.AsEnumerable());
                }
            }

            return proxyRequest;
        }

        /// <summary>
        /// Sends a proxy request and retrieves the response from the target server.
        /// </summary>
        public static async Task<HttpResponseMessage> SendProxyRequest(HttpRequestData req, string targetUrl)
        {
            var proxyRequest = CreateProxyRequest(req, targetUrl);
            var httpClient = HttpClientFactoryProvider.Instance.CreateClient("IgnoreSSL");

            try
            {
                var proxyResponse = await httpClient.SendAsync(proxyRequest);
                var responseBody = await proxyResponse.Content.ReadAsStringAsync();

                // ✅ Log request & response details
                var headers = string.Join(", ", proxyRequest.Headers.Select(x => x.Key));
                Console.WriteLine($"🔹 Sent Request to {targetUrl} | Headers: {headers}");
                Console.WriteLine($"✅ Response Status: {proxyResponse.StatusCode}");
                Console.WriteLine($"🔹 Response Body: {responseBody}");

                return proxyResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to connect to {targetUrl}: {ex.Message}");
                throw;
            }
        }
    }
}
