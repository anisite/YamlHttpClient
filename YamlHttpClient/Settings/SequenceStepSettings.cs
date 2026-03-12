using YamlDotNet.Serialization;

namespace YamlHttpClient.Settings
{
    /// <summary>
    /// One step in an <see cref="HttpClientSetSettings"/> sequence.
    /// </summary>
    public class SequenceStepSettings
    {
        /// <summary>
        /// Key matching an entry in <c>http_client</c>.
        /// </summary>
        [YamlMember(Alias = "http_client")]
        public string HttpClient { get; set; } = default!;

        /// <summary>
        /// Alias used to reference this step's response in subsequent templates.
        /// Defaults to the value of <see cref="HttpClient"/> if not specified.
        /// </summary>
        [YamlMember(Alias = "as")]
        public string? As { get; set; }
    }
}