using Microsoft.Azure.Functions.Worker.Http;

namespace CORS_Proxy_Azure_Function_App.Helpers
{

    public static class CorsHelper
    {
        public static void ApplyCorsHeaders(HttpResponseData response, HttpRequestData request)
        {
            // Allow all origins
            response.Headers.Add("Access-Control-Allow-Origin", "*");

            // Handle preflight (OPTIONS) requests
            if (request.Method == "OPTIONS" && request.Headers.TryGetValues("Access-Control-Max-Age", out var maxAge))
            {
                response.Headers.Add("Access-Control-Max-Age", maxAge);
            }

            // Allow specific methods requested
            if (request.Headers.TryGetValues("Access-Control-Request-Method", out var requestMethod))
            {
                response.Headers.Add("Access-Control-Allow-Methods", requestMethod);
            }

            // Allow specific headers requested
            if (request.Headers.TryGetValues("Access-Control-Request-Headers", out var requestHeaders))
            {
                response.Headers.Add("Access-Control-Allow-Headers", requestHeaders);
            }

            // Expose all response headers
            response.Headers.Add("Access-Control-Expose-Headers", string.Join(",", response.Headers.Select(h => h.Key)));
        }
    }

}
