using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace YamlHttpClient
{
    public class YamlHttpClientConfig
    {
        [YamlMember(Alias = "http_client")]
        public Dictionary<string, HttpClientSettings> HttpClient { get; set; }
    }
}