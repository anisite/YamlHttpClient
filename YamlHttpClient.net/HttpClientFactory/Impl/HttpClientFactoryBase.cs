using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using YamlHttpClient;

namespace YamlHttpClient.Factory
{
    public abstract class YamlHttpClientFactoryBase : IYamlHttpClientFactory
    {
        private readonly ConcurrentDictionary<string, IYamlHttpClient> _clients = new ConcurrentDictionary<string, IYamlHttpClient>();
        private readonly TimeSpan _defaultClientTimeout = TimeSpan.FromSeconds(100);// same as HttpClient default value

        protected YamlHttpClientFactoryBase()
        {
        }
        protected YamlHttpClientFactoryBase(TimeSpan defaultClientTimeout)
        {
            _defaultClientTimeout = defaultClientTimeout;
        }

        public virtual HttpClient GetHttpClient(string url = null)
        {
           var safeClient = _clients.AddOrUpdate(
                GetCacheKey(),
                Create(url),
                (u, client) => client.IsDisposed ? Create(url) : client);

            return safeClient.HttpClient;
        }

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

        public void Dispose()
        {
            foreach (var kv in _clients)
            {
                if (!kv.Value.IsDisposed)
                    kv.Value.Dispose();
            }
            _clients.Clear();
        }


        protected abstract string GetCacheKey();

        protected virtual IYamlHttpClient Create(string url) => new YamlSafeHttpClient(this,url);
        protected virtual IYamlHttpClient CreateProxied(string proxyUrl) => new YamlSafeHttpClient(this, proxyUrl, true);


        internal  HttpClient CreateHttpClientInternal(HttpMessageHandler handler)
        {
            return CreateHttpClient(handler);
        }

        internal HttpMessageHandler CreateMessageHandlerInternal(string proxyUrl = null)
        {
            return CreateMessageHandler(proxyUrl);
        }

        protected virtual HttpClient CreateHttpClient(HttpMessageHandler handler)
        {
            return new HttpClient(handler)
            {
                Timeout = _defaultClientTimeout
            };
        }

        protected virtual HttpMessageHandler CreateMessageHandler(string proxyUrl = null)
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
