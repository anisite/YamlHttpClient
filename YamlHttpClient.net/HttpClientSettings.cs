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
        public string Method { get; set; }
        public string Url { get; set; }
        public string Encoding { get; set; }
        public string StringContent { get; set; }
        [YamlMember(serializeAs: typeof(string))]
        public object JsonContent { get; set; }
        public object FormContent { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }
}