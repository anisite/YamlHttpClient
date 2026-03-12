using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace YamlHttpClient.Settings
{
    /// <summary>
    /// A named pipeline under <c>http_client_set</c>: an ordered sequence of HTTP calls
    /// and an optional Handlebars output template.
    /// </summary>
    public class HttpClientSetSettings
    {
        /// <summary>
        /// Ordered list of HTTP client steps to execute.
        /// </summary>
        [YamlMember(Alias = "sequence")]
        public List<SequenceStepSettings> Sequence { get; set; } = new List<SequenceStepSettings>();

        /// <summary>
        /// Optional output template applied after all steps have completed.
        /// </summary>
        [YamlMember(Alias = "data_adapter")]
        public DataAdapterSettings? DataAdapter { get; set; }
    }
}