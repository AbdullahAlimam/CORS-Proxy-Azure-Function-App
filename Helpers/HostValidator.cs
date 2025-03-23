using System;
using System.Net;
using System.Text.RegularExpressions;

namespace CORSProxy.Helpers
{
    /// <summary>
    /// Provides validation methods for hostnames, including IPv4 and IPv6 address validation.
    /// </summary>
    public static class HostValidator
    {
        /// <summary>
        /// Determines whether the specified hostname is a valid host.
        /// A valid host can be either a valid IPv4 or IPv6 address.
        /// </summary>
        /// <param name="hostname">The hostname or IP address to validate.</param>
        /// <returns><c>true</c> if the hostname is a valid IPv4 or IPv6 address; otherwise, <c>false</c>.</returns>
        public static bool IsValidHost(string hostname)
        {
            if (string.IsNullOrEmpty(hostname))
            {
                return false;
            }

            // ✅ Check if hostname is a valid IPv4 or IPv6 address
            return IsValidIPv4(hostname) || IsValidIPv6(hostname);
        }

        /// <summary>
        /// Validates whether the given hostname is a valid IPv4 address.
        /// </summary>
        /// <param name="hostname">The hostname to check.</param>
        /// <returns><c>true</c> if the hostname is a valid IPv4 address; otherwise, <c>false</c>.</returns>
        public static bool IsValidIPv4(string hostname)
        {
            return IPAddress.TryParse(hostname, out var address) && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        }

        /// <summary>
        /// Validates whether the given hostname is a valid IPv6 address.
        /// </summary>
        /// <param name="hostname">The hostname to check.</param>
        /// <returns><c>true</c> if the hostname is a valid IPv6 address; otherwise, <c>false</c>.</returns>
        public static bool IsValidIPv6(string hostname)
        {
            return IPAddress.TryParse(hostname, out var address) && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;
        }
    }
}
