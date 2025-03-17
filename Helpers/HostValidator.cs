using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CORS_Proxy_Azure_Function_App.Helpers
{
    public class HostValidator
    {
        private static readonly Regex TldRegex = new(@"^(?:com|net|org|edu|gov|mil|[a-z]{2})$", RegexOptions.IgnoreCase);

        public static bool IsValidHost(string hostname)
        {
            if (string.IsNullOrEmpty(hostname))
            {
                return false;
            }

            // ✅ Check if hostname has a valid TLD
            if (TldValidator.IsValidTld(hostname))
            {
                return true;
            }

            // ✅ Check if hostname is a valid IPv4 or IPv6 address
            return IsValidIPv4(hostname) || IsValidIPv6(hostname);
        }

        private static bool IsValidIPv4(string hostname)
        {
            return IPAddress.TryParse(hostname, out var address) && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        }

        private static bool IsValidIPv6(string hostname)
        {
            return IPAddress.TryParse(hostname, out var address) && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;
        }
    }
}
