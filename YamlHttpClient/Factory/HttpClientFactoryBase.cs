using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using YamlHttpClient;
using YamlHttpClient.Interfaces;

namespace YamlHttpClient.Factory
{
    /// <summary>
    /// Base class of factory
    /// </summary>
    public abstract class YamlHttpClientFactoryBase : IYamlHttpClientFactory
    {
        private readonly ConcurrentDictionary<string, IYamlHttpClient> _clients = new ConcurrentDictionary<string, IYamlHttpClient>();
        public TimeSpan DefaultClientTimeout { get; set; } = TimeSpan.FromSeconds(100);// same as HttpClient default value

        /// <summary>
        /// Ctor of base class
        /// </summary>
        protected YamlHttpClientFactoryBase()
        {
        }

        /// <summary>
        /// Ctor of base class
        /// </summary>
        /// <param name="defaultClientTimeout">Delay before timeout</param>
        protected YamlHttpClientFactoryBase(TimeSpan defaultClientTimeout)
        {
            DefaultClientTimeout = defaultClientTimeout;
        }

        /// <summary>
        /// Get http client from cache or instanciate another.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual HttpClient GetHttpClient(string? url = null)
        {
            var safeClient = _clients.AddOrUpdate(
                 GetCacheKey(),
                 Create(url),
                 (u, client) => client.IsDisposed ? Create(url) : client);

            return safeClient.HttpClient;
        }

        /// <summary>
        /// Get http client from cache or instanciate another.
        /// </summary>
        /// <param name="proxyUrl">Specify proxy url to use.</param>
        /// <returns></returns>
        public virtual HttpClient GetProxiedHttpClient(string proxyUrl)
        {
            if (string.IsNullOrEmpty(proxyUrl))
                throw new ArgumentNullException(nameof(proxyUrl));

            var safeClient = _clients.AddOrUpdate(
                proxyUrl,
                CreateProxied,
                (u, client) => client.IsDisposed ? CreateProxied(u) : client);

            return safeClient.HttpClient;
        }

        /// <summary />
        public void Dispose()
        {
            foreach (var kv in _clients)
            {
                if (!kv.Value.IsDisposed)
                    kv.Value.Dispose();
            }
            _clients.Clear();
        }

        /// <summary />
        protected abstract string GetCacheKey();
        /// <summary />
        protected virtual IYamlHttpClient Create(string? url) => new YamlSafeHttpClient(this, url);
        /// <summary />
        protected virtual IYamlHttpClient CreateProxied(string proxyUrl) => new YamlSafeHttpClient(this, proxyUrl, true);


        internal HttpClient CreateHttpClientInternal(HttpMessageHandler handler)
        {
            return CreateHttpClient(handler);
        }

        internal HttpMessageHandler CreateMessageHandlerInternal(string? proxyUrl = null)
        {
            return CreateMessageHandler(proxyUrl);
        }

        /// <summary />
        protected virtual HttpClient CreateHttpClient(HttpMessageHandler handler)
        {
            return new HttpClient(handler)
            {
                Timeout = DefaultClientTimeout
            };
        }

        /// <summary />
        protected virtual HttpMessageHandler CreateMessageHandler(string? proxyUrl = null)
        {
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                return new HttpClientHandler
                {
                    UseProxy = true,
                    Proxy = new WebProxy(proxyUrl),
                    AutomaticDecompression = DecompressionMethods.None
                };
            }
            return new HttpClientHandler
            {
                UseProxy = false,
                AutomaticDecompression = DecompressionMethods.None
            };
        }
    }
}
