using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace YamlHttpClient.Settings
{
    public class RetrySettings
    {
        [YamlMember(Alias = "max_retries")]
        public int MaxRetries { get; set; } = 3;

        [YamlMember(Alias = "delay_milliseconds")]
        public int DelayMilliseconds { get; set; } = 1000;

        [YamlMember(Alias = "retry_on_status_codes")]
        public List<int>? RetryOnStatusCodes { get; set; }
    }
}