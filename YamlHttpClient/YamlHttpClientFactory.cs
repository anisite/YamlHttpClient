using HandlebarsDotNet;
using HandlebarsDotNet.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlHttpClient.Exceptions;
using YamlHttpClient.Factory;
using YamlHttpClient.Interfaces;
using YamlHttpClient.Settings;
using YamlHttpClient.Utils;

namespace YamlHttpClient
{
    /// <summary>
    /// Yaml config based HttpClient
    /// </summary>
    public class YamlHttpClientFactory : YamlHttpClientFactoryBase, IYamlHttpClientAccessor
    {
        private string _uniqueId => HttpClientSettings.Url;
        private readonly IContentHandler _contentHandler;

        /// <summary>
        /// Config
        /// </summary>
        public HttpClientSettings HttpClientSettings { get; set; }

        /// <summary>
        /// Handlebars
        /// </summary>
        public IHandlebars HandlebarsProvider { get; set; }

        public YamlHttpClientFactory()
        {
            HandlebarsProvider = CreateDefaultHandleBars();
        }

        /// <summary>
        /// 
        /// </summary>
        public YamlHttpClientFactory(HttpClientSettings httpClientSettings, IHandlebars? handlebars = null)
        {
            HttpClientSettings = httpClientSettings;
            HandlebarsProvider = handlebars ?? CreateDefaultHandleBars();
            _contentHandler = new ContentHandler(HandlebarsProvider);
        }

        /// <summary>
        /// Create a new instance of YamlHttpClientFactory
        /// </summary>
        /// <param name="httpClientSettings"></param>
        /// <param name="handlebars"></param>
        /// <param name="defaultClientTimeout"></param>
        public YamlHttpClientFactory(HttpClientSettings httpClientSettings,
                                     TimeSpan defaultClientTimeout, 
                                     IHandlebars? handlebars = null) : base(defaultClientTimeout)
        {
            HttpClientSettings = httpClientSettings;
            HandlebarsProvider = handlebars ?? CreateDefaultHandleBars();
            _contentHandler = new ContentHandler(HandlebarsProvider);
        }

        /// <summary>
        /// </summary>
        public static IHandlebars CreateDefaultHandleBars()
        {
            IHandlebars hb;
            hb = Handlebars.Create();

            hb.AddJsonHelper();
            hb.AddBase64();
            hb.AddIfCond(false);

            return hb;
        }

        /// <summary>
        /// </summary>
        public static IHandlebars CreateEmptyHandleBars()
        {
            IHandlebars hb;
            hb = Handlebars.Create();

            return hb;
        }

        /// <summary>
        /// Cache key
        /// </summary>
        /// <returns></returns>
        protected override string GetCacheKey()
        {
            return _uniqueId;
        }

        /// <summary>
        /// Create
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected override IYamlHttpClient Create(string? url)
        {
            return new YamlSafeHttpClient(this, url);
        }

        /// <summary>
        /// Build request message
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public HttpRequestMessage BuildRequestMessage(dynamic data)
        {
            try
            {
                var msg = new HttpRequestMessage(new HttpMethod(HttpClientSettings.Method),
                                                            SS(HttpClientSettings.Url, data));

                msg.Content = (_contentHandler ?? new ContentHandler(HandlebarsProvider)).Content(data, HttpClientSettings.Content);

                // Check If Basic authentication 
                if (HttpClientSettings.AuthBasic is { })
                {
                    var basicAuth = Encoding.ASCII.GetBytes(HttpClientSettings.AuthBasic);
                    msg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(basicAuth));
                }

                // Adding all headers
                if (HttpClientSettings.Headers is { })
                {
                    foreach (var item in HttpClientSettings.Headers)
                    {
                        msg.Headers.TryAddWithoutValidation(item.Key, SS(item.Value, data));
                    }
                }

                return msg;
            }
            catch (UriFormatException ex)
            {
                throw new YamlHttpClientException($"Invalid URI : '{SS(HttpClientSettings.Url, data)}'", ex);
            }
        }

        /// <summary>
        /// Send http request to server with parameters from config
        /// </summary>
        /// <param name="httpRequestMessage"></param>
        /// <returns></returns>
        public virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage)
        {
            var client = GetHttpClient();

            return client.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        /// <summary>
        /// Send http request to server with parameters from config
        /// </summary>
        /// <param name="httpRequestMessage"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
        {
            var client = GetHttpClient();

            return client.SendAsync(httpRequestMessage, cancellationToken);
        }

        /// <summary>
        /// Auto call a web url with settings from config and any data to work with.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> AutoCallAsync(dynamic data)
        {
            var msg = BuildRequestMessage(data);
            return SendAsync(msg, CancellationToken.None);
        }

        /// <summary>
        /// Auto call a web url with settings from config and any data to work with.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> AutoCallAsync(dynamic data, CancellationToken cancellationToken)
        {
            var msg = BuildRequestMessage(data);
            return SendAsync(msg, cancellationToken);
        }


        /// <summary>
        /// Check response from check_response settings in yaml config
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public Task CheckResponseAsync(HttpResponseMessage response)
        {
            return CheckResponseAsync(response, CancellationToken.None);
        }

        /// <summary>
        /// Check response from check_response settings in yaml config
        /// </summary>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task CheckResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (HttpClientSettings.CheckResponse?.ThrowExceptionIfBodyContainsAny?.Any() ?? false)
            {
                foreach (var item in HttpClientSettings.CheckResponse.ThrowExceptionIfBodyContainsAny)
                {
#if NETCOREAPP3_1
                    if ((await response.Content.ReadAsStringAsync()).Contains(item))
                    {
                        throw new ThrowExceptionIfBodyContainsAny(item);
                    }
#else
              if ((await response.Content.ReadAsStringAsync(cancellationToken)).Contains(item))
                    {
                        throw new ThrowExceptionIfBodyContainsAny(item);
                    }
#endif
                }
            }

            if (HttpClientSettings.CheckResponse?.ThrowExceptionIfBodyNotContainAll?.Any() ?? false)
            {
                foreach (var item in HttpClientSettings.CheckResponse.ThrowExceptionIfBodyNotContainAll)
                {
                    if (!(await response.Content.ReadAsStringAsync()).Contains(item))
                    {
                        throw new ThrowExceptionIfBodyNotContainAll(item);
                    }
                }
            }

        }

        /// <summary>
        /// Message handler
        /// </summary>
        /// <param name="proxyUrl"></param>
        /// <returns></returns>
        protected override HttpMessageHandler CreateMessageHandler(string? proxyUrl = null)
        {
            var handler = new HttpClientHandler();

            if (!string.IsNullOrEmpty(proxyUrl))
            {
                handler = new HttpClientHandler
                {
                    UseProxy = true,
                    Proxy = new WebProxy(proxyUrl),
                    //AutomaticDecompression = DecompressionMethods.None
                };
            }

            handler.UseDefaultCredentials = HttpClientSettings.UseDefaultCredentials;
            handler.AllowAutoRedirect = true;

            return handler;
        }

        /// <summary>
        /// Sustitute data with Stubble
        /// </summary>
        /// <param name="value">Value with placeholders {{example}}</param>
        /// <param name="data">Any data object to search from</param>
        /// <returns></returns>
        private string SS(string value, object data)
        {
            var comp = HandlebarsProvider.Compile(value);
            return comp(data);
        }
    }
}
