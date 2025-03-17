using Microsoft.Azure.Functions.Worker.Http;
using System.Linq;

namespace CORSProxy.Helpers
{
    /// <summary>
    /// Provides helper methods for handling Cross-Origin Resource Sharing (CORS) in Azure Functions.
    /// </summary>
    public static class CorsHelper
    {
        /// <summary>
        /// Applies CORS headers to an HTTP response based on the incoming request.
        /// </summary>
        /// <param name="response">The HTTP response to modify.</param>
        /// <param name="request">The incoming HTTP request containing CORS headers.</param>
        public static void ApplyCorsHeaders(HttpResponseData response, HttpRequestData request)
        {
            // ✅ Allow all origins (can be modified to use a whitelist)
            response.Headers.Add("Access-Control-Allow-Origin", "*");

            // ✅ Handle preflight (OPTIONS) requests
            if (request.Method == "OPTIONS")
            {
                if (request.Headers.TryGetValues("Access-Control-Max-Age", out var maxAge))
                {
                    response.Headers.Add("Access-Control-Max-Age", maxAge);
                }

                if (request.Headers.TryGetValues("Access-Control-Request-Method", out var requestMethod))
                {
                    response.Headers.Add("Access-Control-Allow-Methods", requestMethod);
                }

                if (request.Headers.TryGetValues("Access-Control-Request-Headers", out var requestHeaders))
                {
                    response.Headers.Add("Access-Control-Allow-Headers", requestHeaders);
                }
            }

            // ✅ Expose all response headers to the client
            if (response.Headers.Count() > 0)
            {
                response.Headers.Add("Access-Control-Expose-Headers", string.Join(",", response.Headers.Select(h => h.Key)));
            }
        }
    }
}
