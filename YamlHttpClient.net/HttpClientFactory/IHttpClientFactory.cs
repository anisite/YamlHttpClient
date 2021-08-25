using System;
using System.Net.Http;

namespace YamlHttpClient
{
    public interface IYamlHttpClientFactory : IDisposable
    {
        HttpClient GetHttpClient(string url);

        HttpClient GetProxiedHttpClient(string proxyUrl);

    }
}
