using Azure;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace CORS_Proxy_Azure_Function_App.Helpers
{
    public static class ExtentionHelper
    {
        public async static Task<HttpResponseData> CreateBadResponse(this HttpRequestData req, string msg, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            var badResponse = req.CreateResponse(statusCode);
            badResponse.Headers.Add("Content-Type", "text/plain");
            await badResponse.WriteStringAsync(msg);
            return badResponse;
        }

        /// <summary>
        /// Creates an error response with a given status code and message.
        /// </summary>
        public static async Task<HttpResponseData> CreateErrorResponse(this HttpRequestData req, string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "text/plain");
            await response.WriteStringAsync(message);
            return response;
        }
    }
}
