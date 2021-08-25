using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Net.Http;
using YamlDotNet.Serialization;

namespace YamlHttpClient
{
    /*
     *   
          backend.client:
              scope: 'https://api\.infologique\.net' #regex de l'url
              network: web # web ou RITM
              #base_uri: 'https://api.infologique.net'
              headers:
                  Accept: 'application/json'
     * */
    public class HttpClientSettings
    {
        [YamlMember(Alias = "method")]
        public string Method { get; set; }
        [YamlMember(Alias = "url")]
        public string Url { get; set; }
        [YamlMember(Alias = "encoding")]
        public string Encoding { get; set; }
        [YamlMember(Alias = "string_content")]
        public string StringContent { get; set; }
        [YamlMember(Alias = "json_content")]
        public string JsonContent { get; set; }
        [YamlMember(Alias = "form_content")]
        public object FormContent { get; set; }
        [YamlMember(Alias = "headers")]
        public Dictionary<string, string> Headers { get; set; }
    }
}