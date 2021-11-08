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
    public class YamlHttpClientFactory : YamlHttpClientFactoryBase
    {
        private readonly HttpClientSettings _config;
        private readonly string _uniqueId;
        private readonly IHandlebars _handlebars;
        private readonly IContentHandler _contentHandler;

        /// <summary>
        /// 
        /// </summary>
        public YamlHttpClientFactory(HttpClientSettings httpClientSettings, JsonSerializerSettings? jsonSerializerSettings = null)
        {
            _uniqueId = httpClientSettings.Url;
            _config = httpClientSettings;
            _handlebars = CreateHandleBars(jsonSerializerSettings);
            _contentHandler = new ContentHandler(_handlebars);
        }

        /// <summary>
        /// Create a new instance of YamlHttpClientFactory
        /// </summary>
        /// <param name="httpClientSettings"></param>
        /// <param name="defaultClientTimeout"></param>
        /// <param name="jsonSerializerSettings"></param>
        public YamlHttpClientFactory(HttpClientSettings httpClientSettings,
                                     TimeSpan defaultClientTimeout,
                                     JsonSerializerSettings? jsonSerializerSettings) : base(defaultClientTimeout)
        {
            _uniqueId = httpClientSettings.Url;
            _config = httpClientSettings;
            _handlebars = CreateHandleBars(jsonSerializerSettings);
            _contentHandler = new ContentHandler(_handlebars);
        }

        /// <summary>
        /// </summary>
        public static IHandlebars CreateHandleBars(JsonSerializerSettings? jsonSerializerSettings = null)
        {
            IHandlebars hb;
            hb = Handlebars.Create();

            hb.AddJsonHelper(jsonSerializerSettings);
            hb.AddBase64();

            return hb;
        }

        /// <summary>
        /// Config
        /// </summary>
        public HttpClientSettings HttpClientSettings => _config;

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
            var msg = new HttpRequestMessage(new HttpMethod(_config.Method),
                                                            SS(_config.Url, data));

            msg.Content = _contentHandler.Content(data, _config.Content);

            // Check If Basic authentication 
            if (_config.AuthBasic is { })
            {
                var basicAuth = Encoding.ASCII.GetBytes(_config.AuthBasic);
                msg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(basicAuth));
            }

            // Adding all headers
            if (_config.Headers is { })
                foreach (var item in _config.Headers)
                {
                    msg.Headers.TryAddWithoutValidation(item.Key, SS(item.Value, data));
                }

            return msg;
        }

        /// <summary>
        /// Send http request to server with parameters from config
        /// </summary>
        /// <param name="httpRequestMessage"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage)
        {
            var client = GetHttpClient();
            return client.SendAsync(httpRequestMessage);
        }

        /// <summary>
        /// Auto call a web url with settings from config and any data to work with.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> AutoCallAsync(dynamic data)
        {
            var msg = BuildRequestMessage(data);
            return SendAsync(msg);
        }

        /// <summary>
        /// Check response from check_response settings in yaml config
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public async Task CheckResponseAsync(HttpResponseMessage response)
        {
            if (_config.CheckResponse?.ThrowExceptionIfBodyContainsAny?.Any() ?? false)
            {
                foreach (var item in _config.CheckResponse.ThrowExceptionIfBodyContainsAny)
                {
                    if ((await response.Content.ReadAsStringAsync()).Contains(item))
                    {
                        throw new ThrowExceptionIfBodyContainsAny(item);
                    }
                }
            }

            if (_config.CheckResponse?.ThrowExceptionIfBodyNotContainAll?.Any() ?? false)
            {
                foreach (var item in _config.CheckResponse.ThrowExceptionIfBodyNotContainAll)
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

            handler.UseDefaultCredentials = _config.UseDefaultCredentials;
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
            var comp = _handlebars.Compile(value);
            return comp(data);
        }
    }
}
