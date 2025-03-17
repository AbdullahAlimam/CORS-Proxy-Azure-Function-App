using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace CORSProxy.Helpers
{
    /// <summary>
    /// Provides a centralized way to access IHttpClientFactory globally.
    /// </summary>
    public static class HttpClientFactoryProvider
    {
        private static IHttpClientFactory? _httpClientFactory;

        /// <summary>
        /// Sets the IHttpClientFactory instance. Should be called once at application startup.
        /// </summary>
        public static void Initialize(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <summary>
        /// Gets the global IHttpClientFactory instance.
        /// </summary>
        public static IHttpClientFactory Instance
        {
            get
            {
                if (_httpClientFactory == null)
                    throw new InvalidOperationException("HttpClientFactoryProvider is not initialized. Call Initialize() first.");

                return _httpClientFactory;
            }
        }
    }
}
