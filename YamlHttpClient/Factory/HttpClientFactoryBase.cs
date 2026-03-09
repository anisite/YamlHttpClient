using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;

namespace YamlHttpClient.Factory
{
    /// <summary>
    /// Base class of factory
    /// </summary>
    public abstract class YamlHttpClientFactoryBase
    {
        private static readonly ConcurrentDictionary<string, HttpClient> _clients = new ConcurrentDictionary<string, HttpClient>();

        public TimeSpan DefaultClientTimeout { get; set; } = TimeSpan.FromSeconds(100);

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
            // On utilise GetOrAdd. Le client créé vivra éternellement, 
            // mais ses connexions internes seront recyclées par le SocketsHttpHandler.
            return _clients.GetOrAdd(GetCacheKey(), _ =>
            {
                var handler = CreateMessageHandlerInternal(null);
                return CreateHttpClientInternal(handler);
            });
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

            // On crée une clé unique pour le proxy
            string cacheKey = $"{GetCacheKey()}_proxy_{proxyUrl}";

            return _clients.GetOrAdd(cacheKey, _ =>
            {
                var handler = CreateMessageHandlerInternal(proxyUrl);
                return CreateHttpClientInternal(handler);
            });
        }

        /// <summary />
        protected abstract string GetCacheKey();

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
            var handler = new SocketsHttpHandler
            {
                // Ferme silencieusement les connexions inactives après 15 minutes pour rafraîchir les DNS
                // sans briser les requêtes en cours.
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                AutomaticDecompression = DecompressionMethods.None
            };

            if (!string.IsNullOrEmpty(proxyUrl))
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(proxyUrl);
            }
            else
            {
                handler.UseProxy = false;
            }

            return handler;
        }
    }
}