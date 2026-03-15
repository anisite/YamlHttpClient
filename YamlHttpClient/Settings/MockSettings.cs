using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace YamlHttpClient.Settings
{
    /// <summary>
    /// Mock settings for simulating HTTP responses without making real network calls.
    /// Useful for automated testing and offline development.
    /// </summary>
    public class MockSettings
    {
        [YamlMember(Alias = "enabled")]
        public bool Enabled { get; set; }

        [YamlMember(Alias = "status_code")]
        public int StatusCode { get; set; } = 200;

        [YamlMember(Alias = "body")]
        public string? Body { get; set; }

        [YamlMember(Alias = "headers")]
        public Dictionary<string, string>? Headers { get; set; }
    }
}