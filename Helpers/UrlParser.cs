using System;
using System.Text.RegularExpressions;

namespace CORS_Proxy_Azure_Function_App.Helpers
{
    public class UrlParser
    {
        /// <summary>
        /// Parses a given URL, ensuring it has a valid scheme and extracts components.
        /// </summary>
        /// <param name="requestedUrl">The requested URL (scheme is optional).</param>
        /// <returns>A Uri object if valid, otherwise null.</returns>
        public static Uri? ParseUrl(string requestedUrl)
        {
            if (string.IsNullOrWhiteSpace(requestedUrl))
            {
                return null;
            }

            // Regex to extract protocol, hostname, port, path
            var match = Regex.Match(requestedUrl,
                @"^(?:(https?:)?\/\/)?(([^\/?]+?)(?::(\d{0,5})(?=[\/?]|$))?)([\/?][\S\s]*|$)",
                RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return null;
            }

            // Extract parts
            string protocol = match.Groups[1].Value;   // "http" / "https"
            string host = match.Groups[2].Value;       // "example.com"
            string hostname = match.Groups[3].Value;   // "example.com" (excluding port)
            string port = match.Groups[4].Value;       // "443"
            string pathAndQuery = match.Groups[5].Value; // "/path?query"

            // If no protocol is provided, assume it
            if (string.IsNullOrEmpty(protocol))
            {
                if (requestedUrl.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
                    requestedUrl.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
                {
                    return null; // Invalid case: "http:///" parsed incorrectly
                }

                // Ensure "//" is present if omitted
                if (!requestedUrl.StartsWith("//"))
                {
                    requestedUrl = "//" + requestedUrl;
                }

                // Assign default protocol based on port (if available)
                requestedUrl = (port == "443" ? "https:" : "http:") + requestedUrl;
            }

            // Try to parse URL using Uri class
            if (Uri.TryCreate(requestedUrl, UriKind.Absolute, out Uri? parsedUri) && !string.IsNullOrEmpty(parsedUri.Host))
            {
                return parsedUri;
            }

            return null;
        }
    }

}
