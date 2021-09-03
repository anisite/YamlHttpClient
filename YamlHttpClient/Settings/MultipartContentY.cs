using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace YamlHttpClient.Settings
{
    public class MultipartContentY
    {
        [YamlMember(Alias = "boundary")]
        public string? Boundary { get; set; }

        [YamlMember(Alias = "contents")]
        public IEnumerable<ContentSettings>? Contents { get; set; }
    }
}