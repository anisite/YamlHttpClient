using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace YamlHttpClient.net
{
    public class YamlHttpClientHandler : HttpClientHandler
    {
        private HttpClientSettings _config;

        public YamlHttpClientHandler(HttpClientSettings config)
        {
            _config = config;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            base.MaxAutomaticRedirections = 5;
            base.AllowAutoRedirect = true;
            base.SslProtocols = System.Security.Authentication.SslProtocols.None;
            base.UseCookies = false;
            base.UseDefaultCredentials = false;
            return base.SendAsync(request, cancellationToken);
        }
    }
}
