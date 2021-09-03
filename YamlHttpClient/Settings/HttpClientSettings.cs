using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Net.Http;
using YamlDotNet.Serialization;

#pragma warning disable CS8618 // Le champ non-nullable n'est pas initialisé. Déclarez-le comme étant nullable.
namespace YamlHttpClient.Settings
{
    /// <summary />
    public class HttpClientSettings
    {
        /// <summary />
        [YamlMember(Alias = "method")]
        public string Method { get; set; }
        /// <summary />
        [YamlMember(Alias = "url")]
        public string Url { get; set; }
        /// <summary />
        [YamlMember(Alias = "encoding")]
        public string Encoding { get; set; }
        /// <summary />
        [YamlMember(Alias = "content")]
        public ContentSettings Content { get; set; }
        /// <summary />
        [YamlMember(Alias = "use_default_credentials")]
        public bool UseDefaultCredentials { get; set; }
        /// <summary />
        [YamlMember(Alias = "headers")]
        public Dictionary<string, string> Headers { get; set; }
        /// <summary />
        [YamlMember(Alias = "check_response")]
        public CheckResponse? CheckResponse { get; set; }
        /// <summary />
        [YamlMember(Alias = "auth_basic")]
        public string? AuthBasic { get; set; }
        /// <summary />
        [YamlIgnore()]
        public string SettingKey { get; set; }
    }
#pragma warning restore CS8618 // Le champ non-nullable n'est pas initialisé. Déclarez-le comme étant nullable.

}