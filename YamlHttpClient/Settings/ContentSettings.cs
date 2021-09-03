using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace YamlHttpClient.Settings
{
    /// <summary />
    public class ContentSettings
    {
        /// <summary />
        [YamlMember(Alias = "name")]
        public string? ContentName { get; set; }
        /// <summary />
        [YamlMember(Alias = "content_type")]
        public string? ContentType { get; set; }
        /// <summary />
        [YamlMember(Alias = "encoding")]
        public string? Encoding { get; set; }
        /// <summary />
        [YamlMember(Alias = "string_content")]
        public string? StringContent { get; set; }
        /// <summary />
        [YamlMember(Alias = "json_content")]
        public string? JsonContent { get; set; }
        /// <summary />
        [YamlMember(Alias = "form_content")]
        public IDictionary<string, string>? FormContent { get; set; }
        /// <summary />
        [YamlMember(Alias = "multipart_content")]
        public MultipartContentY? MultipartContent { get; internal set; }
        /// <summary />
        [YamlMember(Alias = "filename")]
        public string? FileName { get; internal set; }
        /// <summary />
        [YamlMember(Alias = "base64_content")]
        public string? Base64Content { get; internal set; }
    }
}