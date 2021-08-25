﻿using Newtonsoft.Json;
using Stubble.Core.Builders;
using Stubble.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YamlHttpClient.Factory;
using YamlHttpClient.Utils;

namespace YamlHttpClient
{
    /// <summary>
    /// Yaml config based HttpClient
    /// </summary>
    public class YamlHttpClient : YamlHttpClientFactoryBase
    {
        private readonly HttpClientSettings _config;
        private readonly string _uniqueId;
        private readonly IStubbleRenderer _stubble;

        public YamlHttpClient(string keyConfigName, string yamlConfig)
        {
            _uniqueId = keyConfigName + yamlConfig;
            _config = LoadConfig(keyConfigName, yamlConfig);
            _stubble = new StubbleBuilder().Build();
        }

        public YamlHttpClient(string keyConfigName, string yamlConfig, TimeSpan defaultClientTimeout) : base(defaultClientTimeout)
        {
            _uniqueId = keyConfigName + yamlConfig;
            _config = LoadConfig(keyConfigName, yamlConfig);
            _stubble = new StubbleBuilder().Build();
        }

        private HttpClientSettings LoadConfig(string keyConfigName, string yamlConfig)
        {
            YamlHttpClientConfig config;
            using (var configFile = new StreamReader(yamlConfig))
            {
                var builder = new YamlDotNet.Serialization.DeserializerBuilder()
                    .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.NullNamingConvention.Instance)
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
                                                            _config.Url);

            // String Content
            if (!string.IsNullOrWhiteSpace(_config.StringContent))
            {
                msg.Content = new StringContent(_config.StringContent);
            }
            // Json Content
            else if (!(_config.JsonContent is null))
            {
                var objet = JsonConvert.DeserializeObject<IDictionary<object, object>>(
                                 _config.JsonContent.ToString() ?? string.Empty,
                                     new JsonConverter[] {
                                         new JsonCustomConverter(_stubble, data) });
                var json = JsonConvert.SerializeObject(objet);
                msg.Content = new StringContent(json, Encoding.GetEncoding(_config.Encoding ?? "UTF-8"), "application/json");
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

        /// <summary>
        /// Sustitute data with Stubble
        /// </summary>
        /// <param name="value">Value with placeholders {{example}}</param>
        /// <param name="data">Any data object to search from</param>
        /// <returns></returns>
        private string SS(string value, object data)
        {
            return _stubble.Render(value, data);
        }
    }
}