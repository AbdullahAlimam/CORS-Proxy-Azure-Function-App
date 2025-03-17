using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

namespace CORSProxy.Helpers
{
    /// <summary>
    /// Provides validation methods for domains, including checking for valid IP addresses and TLDs.
    /// </summary>
    public static class DomainValidator
    {
        /// <summary>
        /// Validates whether the given input is a valid domain.
        /// A valid domain can be either an IP address (IPv4/IPv6) or a domain with a valid TLD.
        /// </summary>
        /// <param name="input">The domain or IP address to validate.</param>
        /// <returns><c>true</c> if the input is a valid domain or IP address; otherwise, <c>false</c>.</returns>
        public static bool IsValidDomain(string input)
        {
            // Check if the input is a valid IP (IPv4/IPv6)
            if (HostValidator.IsValidHost(input))
            {
                return true;
            }

            // Check if the input has a valid Top-Level Domain (TLD)
            return TldValidator.IsValidTld(input);
        }
    }
}

