using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace YamlHttpClient.net
{
    public class YamlHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClientSettings _config;

        // Constructor
        public YamlHttpClient(string clientName, string yamlConfig)
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

            _httpClient = new HttpClient(new YamlHttpClientHandler(_config));
            _config = config.HttpClient[clientName];
        }

        public Task<HttpResponseMessage> SendAsync()
        {

            var msg = new HttpRequestMessage(new HttpMethod(_config.Method),
                                                _config.Url);

            msg.Content = new StringContent(_config.StringContent);

            foreach (var item in _config.Headers)
            {
                msg.Headers.TryAddWithoutValidation(item.Key, item.Value);
            }

            return _httpClient.SendAsync(msg);
        }

    }
}
