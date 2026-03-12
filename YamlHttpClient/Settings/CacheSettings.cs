using YamlDotNet.Serialization;

namespace YamlHttpClient.Settings
{
    public class CacheSettings
    {
        [YamlMember(Alias = "enabled")]
        public bool Enabled { get; set; }

        [YamlMember(Alias = "ttl_seconds")]
        public int TtlSeconds { get; set; } = 600; // 10 minutes par défaut
    }
}