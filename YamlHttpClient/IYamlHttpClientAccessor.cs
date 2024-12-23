using HandlebarsDotNet;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using YamlHttpClient.Settings;

namespace YamlHttpClient
{
    public interface IYamlHttpClientAccessor
    {
        public HttpClientSettings HttpClientSettings { get; set; }
        public IHandlebars HandlebarsProvider { get; set; }
        public TimeSpan DefaultClientTimeout { get; set; }
        public HttpRequestMessage BuildRequestMessage(dynamic data);
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage);
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken);
        public Task<HttpResponseMessage> AutoCallAsync(dynamic data);
        public Task<HttpResponseMessage> AutoCallAsync(dynamic data, CancellationToken cancellationToken);
        public Task CheckResponseAsync(HttpResponseMessage response);
        public Task CheckResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken);
    }
}