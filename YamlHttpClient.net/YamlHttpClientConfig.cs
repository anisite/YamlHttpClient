using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace YamlHttpClient
{
    public class YamlHttpClientConfig
    {
        public Dictionary<string, HttpClientSettings> HttpClient { get; set; }
    }
}