using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace YamlHttpClient.Settings
{
    /// <summary>
    /// Config builder
    /// </summary>
    public class YamlHttpClientConfigBuilder
    {
        /// <summary>
        /// Individual HTTP client definitions, keyed by name.
        /// </summary>
        [YamlMember(Alias = "http_client")]
        public Dictionary<string, HttpClientSettings> HttpClient { get; set; } = default!;

        /// <summary>
        /// Named pipelines, each composed of an ordered sequence of HTTP client steps
        /// and an optional Handlebars output template.
        /// </summary>
        [YamlMember(Alias = "http_client_set")]
        public Dictionary<string, HttpClientSetSettings>? HttpClientSet { get; set; }
    }
}