using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

namespace CORSProxy.Helpers
{
    /// <summary>
    /// Handles origin validation for CORS requests based on whitelists and blacklists.
    /// Ensures that requests comply with allowed or blocked origins.
    /// </summary>
    public static class OriginValidator
    {
        /// <summary>
        /// A list of blacklisted origins that are explicitly blocked from making requests.
        /// </summary>
        public static string[] OriginBlacklist = Array.Empty<string>(); // Example: { "http://bad-origin.com" }
        
        /// <summary>
        /// A list of whitelisted origins that are explicitly allowed to make requests.
        /// If empty, all origins are allowed.
        /// </summary>
        public static string[] OriginWhitelist = Array.Empty<string>(); // Example: { "http://allowed-origin.com" }

        /// <summary>
        /// Loads the origin whitelist and blacklist configurations from the application settings.
        /// </summary>
        /// <param name="config">The configuration provider containing CORS settings.</param>
        public static void LoadConfiguration(IConfiguration config)
        {
            OriginBlacklist = config["CORS_BLACKLIST"]?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            OriginWhitelist = config["CORS_WHITELIST"]?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        }

        /// <summary>
        /// Determines whether the request's origin is allowed based on the whitelist.
        /// </summary>
        /// <param name="req">The incoming HTTP request.</param>
        /// <returns><c>true</c> if the request origin is in the whitelist or if the whitelist is empty; otherwise, <c>false</c>.</returns>
        public static bool IsOriginAllowed(HttpRequestData req)
        {
            if (OriginWhitelist.Length == 0) return true;

            if (req.Headers.TryGetValues("Origin", out var origins))
            {
                return origins.Any(origin => OriginWhitelist.Contains(origin));
            }

            return false;
        }

        /// <summary>
        /// Determines whether the request's origin is blocked based on the blacklist.
        /// </summary>
        /// <param name="req">The incoming HTTP request.</param>
        /// <returns><c>true</c> if the request origin is in the blacklist; otherwise, <c>false</c>.</returns>
        public static bool IsOriginBlocked(HttpRequestData req)
        {
            if (OriginBlacklist.Length == 0) return false;

            if (req.Headers.TryGetValues("Origin", out var origins))
            {
                return origins.Any(origin => OriginBlacklist.Contains(origin));
            }

            return false;
        }
    }
}
