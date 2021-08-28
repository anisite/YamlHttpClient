using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace YamlHttpClient.Settings
{
    /// <summary>
    /// Config builder
    /// </summary>
    public class YamlHttpClientConfigBuilder
    {
        /// <summary />
        [YamlMember(Alias = "http_client")]
        public Dictionary<string, HttpClientSettings> HttpClient { get; set; } = default!;
    }
}