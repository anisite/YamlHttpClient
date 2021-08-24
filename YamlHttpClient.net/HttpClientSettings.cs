using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Net.Http;

namespace YamlHttpClient.net
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
        public string StringContent { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }
}