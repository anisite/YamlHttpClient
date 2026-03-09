using System.Collections.Generic;

namespace YamlHttpClient.Settings
{
    /// <summary>
    /// Global options and fallback settings for the YamlHttpOrchestrator.
    /// </summary>
    public class YamlHttpOrchestratorOptions
    {
        public CacheSettings DefaultCacheSettings { get; set; } = new CacheSettings
        {
            Enabled = true,
            TtlSeconds = 1200
        };

        public RetrySettings DefaultRetrySettings { get; set; } = new RetrySettings
        {
            MaxRetries = 3,
            RetryOnStatusCodes = new List<int> { 500, 501, 502, 503, 504 }
        };

        public string HandlebarsEngineKey { get; set; } = "default_handlebars_engine";
    }
}