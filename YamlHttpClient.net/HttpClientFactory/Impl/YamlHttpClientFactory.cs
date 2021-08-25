using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace YamlHttpClient.Factory
{
    /// <summary>
    /// same url use same HttpClient
    /// </summary>
    public class YamlHttpClientFactory : YamlHttpClientFactoryBase
    {
        private readonly HttpClientSettings _config;
        private readonly string _uniqueId;

        public YamlHttpClientFactory(string keyConfigName, string yamlConfig)
        {
            _uniqueId = keyConfigName + yamlConfig;
            _config = LoadConfig(keyConfigName, yamlConfig);
        }

        public YamlHttpClientFactory(string keyConfigName, string yamlConfig, TimeSpan defaultClientTimeout) : base(defaultClientTimeout)
        {
            _uniqueId = keyConfigName + yamlConfig;
            _config = LoadConfig(keyConfigName, yamlConfig);
        }

        private HttpClientSettings LoadConfig(string keyConfigName, string yamlConfig)
        {
            YamlHttpClientConfig config;
            using (var configFile = new StreamReader(yamlConfig))
            {
                var builder = new YamlDotNet.Serialization.DeserializerBuilder()
                    .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
                    .Build();

                config = builder
                    .Deserialize<YamlHttpClientConfig>(configFile);
            }

            //_httpClient = new HttpClient(new YamlHttpClientHandler(_config));
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

            if(!string.IsNullOrWhiteSpace(_config.StringContent))
            {
                msg.Content = new StringContent(_config.StringContent);
            }

            if (!(_config.JsonContent is null))
            {
                //TODO custom JSON serializer pour remplacer les {templates}
                var json = JsonConvert.SerializeObject(_config.JsonContent);
                msg.Content = new StringContent(json, Encoding.GetEncoding(_config.Encoding ?? "UTF-8"), "application/json");
            }
            msg.Content = new MultipartFormDataContent().Add(;
            foreach (var item in _config.Headers)
            {
                msg.Headers.TryAddWithoutValidation(item.Key, item.Value);
            }

            return client.SendAsync(msg);
        }
    }
}
