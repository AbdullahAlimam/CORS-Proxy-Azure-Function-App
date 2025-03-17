using Azure;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace CORSProxy.Helpers
{
    /// <summary>
    /// Provides extension methods for creating consistent HTTP responses in Azure Functions.
    /// </summary>
    public static class HttpResponseExtensions
    {
        /// <summary>
        /// Creates a standardized bad request response with a custom message.
        /// </summary>
        /// <param name="req">The incoming HTTP request.</param>
        /// <param name="message">The error message to include in the response.</param>
        /// <param name="statusCode">The HTTP status code (defaults to 400 Bad Request).</param>
        /// <returns>An <see cref="HttpResponseData"/> object containing the error response.</returns>
        public static async Task<HttpResponseData> CreateBadRequestResponse(
            this HttpRequestData req,
            string message,
            HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "text/plain");
            await response.WriteStringAsync(message);
            return response;
        }

        /// <summary>
        /// Creates a standardized error response with a given status code and message.
        /// </summary>
        /// <param name="req">The incoming HTTP request.</param>
        /// <param name="message">The error message to include in the response.</param>
        /// <param name="statusCode">The HTTP status code (defaults to 500 Internal Server Error).</param>
        /// <returns>An <see cref="HttpResponseData"/> object containing the error response.</returns>
        public static async Task<HttpResponseData> CreateErrorResponse(
            this HttpRequestData req,
            string message,
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "text/plain");
            await response.WriteStringAsync(message);
            return response;
        }
    }
}
