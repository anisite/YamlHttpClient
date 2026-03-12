using YamlDotNet.Serialization;

namespace YamlHttpClient.Settings
{
    /// <summary>
    /// Handlebars output template applied after all steps of a sequence have completed.
    /// </summary>
    public class DataAdapterSettings
    {
        /// <summary>
        /// Handlebars template string. Has access to <c>input</c> and every step alias.
        /// If null or whitespace, the full aggregated data object is returned as raw JSON.
        /// </summary>
        [YamlMember(Alias = "template")]
        public string? Template { get; set; }
    }
}