using YamlDotNet.Serialization;

namespace YamlHttpClient.Settings
{
    public class ChaosSettings
    {
        [YamlMember(Alias = "enabled")]
        public bool Enabled { get; set; }

        [YamlMember(Alias = "injection_rate_percentage")]
        public int InjectionRatePercentage { get; set; } = 33;

        [YamlMember(Alias = "delay_milliseconds")]
        public int? DelayMilliseconds { get; set; }

        [YamlMember(Alias = "simulate_status_code")]
        public int? SimulateStatusCode { get; set; }

        [YamlMember(Alias = "simulate_network_exception")]
        public bool SimulateNetworkException { get; set; }
    }
}