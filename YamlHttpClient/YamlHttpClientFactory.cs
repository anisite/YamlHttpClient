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

        /// <summary>
        /// 
        /// </summary>
        public YamlHttpClientFactory(HttpClientSettings httpClientSettings)
        {
            _uniqueId = httpClientSettings.Url;
            _config = httpClientSettings;
            _handlebars = CreateHandleBars();
        }

        /// <summary>
        /// Create a new instance of YamlHttpClientFactory
        /// </summary>
        /// <param name="httpClientSettings"></param>
        /// <param name="defaultClientTimeout"></param>
        public YamlHttpClientFactory(HttpClientSettings httpClientSettings, TimeSpan defaultClientTimeout) : base(defaultClientTimeout)
        {
            _uniqueId = httpClientSettings.Url;
            _config = httpClientSettings;
            _handlebars = CreateHandleBars();
        }

        private IHandlebars CreateHandleBars()
        {
            IHandlebars hb;
            hb = Handlebars.Create();

            hb.AddJsonHelper();

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

            // String Content
            if (!string.IsNullOrWhiteSpace(_config.StringContent))
            {
                msg.Content = new StringContent(_config.StringContent);
            }
            // Json Content
            else if (!(_config.JsonContent is null))
            {
                //var tt = SS(_config.JsonContent.ToString() ?? string.Empty, data);

                var template = _handlebars.Compile(_config.JsonContent.ToString() ?? string.Empty);

                var result = template(data);

                /*var objet = JsonConvert.DeserializeObject<IDictionary<object, object>>(
                                 SS(_config.JsonContent.ToString() ?? string.Empty, data),
                                     new JsonConverter[] {
                                         new JsonCustomConverter(_stubble, data) });*/

                //var json = JsonConvert.SerializeObject(objet);
                msg.Content = new StringContent(result, Encoding.GetEncoding(_config.Encoding ?? "UTF-8"), "application/json");
            }
            else
            {
                throw new NotImplementedException("Json or String content is mandatory.");
            }

            // Adding all headers
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
