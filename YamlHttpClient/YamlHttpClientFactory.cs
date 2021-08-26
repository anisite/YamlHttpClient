using HandlebarsDotNet;
using HandlebarsDotNet.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
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

        public YamlHttpClientFactory(string keyConfigName, byte[] yamlConfig)
        {
            _uniqueId = keyConfigName + yamlConfig;
            _config = ReadFromBytes(keyConfigName, yamlConfig);
            _handlebars = CreateHandleBars();
        }

        public YamlHttpClientFactory(string keyConfigName, string yamlConfig)
        {
            _uniqueId = keyConfigName + yamlConfig;
            _config = ReadFromFile(keyConfigName, yamlConfig);
            _handlebars = CreateHandleBars();
        }

        public YamlHttpClientFactory(string keyConfigName, string yamlConfig, TimeSpan defaultClientTimeout) : base(defaultClientTimeout)
        {
            _uniqueId = keyConfigName + yamlConfig;
            _config = ReadFromFile(keyConfigName, yamlConfig);
            _handlebars = CreateHandleBars();
        }

        private IHandlebars CreateHandleBars()
        {
            IHandlebars hb;
            var formatter = new CustomJsonFormatter();
            hb = Handlebars.Create();
            hb.Configuration.FormatterProviders.Add(formatter);
            return hb;
        }

        private HttpClientSettings ReadFromBytes(string keyConfigName, byte[] yamlFile)
        {
            YamlHttpClientConfig config;

            var builder = new DeserializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
                .Build();

            config = builder
                .Deserialize<YamlHttpClientConfig>(Encoding.Default.GetString(yamlFile));

            return config.HttpClient[keyConfigName];
        }

        private HttpClientSettings ReadFromFile(string keyConfigName, string filePath)
        {
            YamlHttpClientConfig config;
            using (var configFile = new StreamReader(filePath))
            {
                var builder = new DeserializerBuilder()
                    .WithNamingConvention(NullNamingConvention.Instance)
                    .Build();

                config = builder
                    .Deserialize<YamlHttpClientConfig>(configFile);
            }

            return config.HttpClient[keyConfigName];
        }

        protected override string GetCacheKey()
        {
            return _uniqueId;
        }

        protected override IYamlHttpClient Create(string url)
        {
            return new YamlSafeHttpClient(this, url);
        }

        public Task<HttpResponseMessage> AutoCall(dynamic data)
        {
            var client = GetHttpClient();
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

            return client.SendAsync(msg);
        }

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
