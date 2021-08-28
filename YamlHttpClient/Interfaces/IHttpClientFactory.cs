using System;
using System.Net.Http;

namespace YamlHttpClient.Interfaces
{
    /// <summary/>
    public interface IYamlHttpClientFactory : IDisposable
    {
        /// <summary/>
        HttpClient GetHttpClient(string url);

        /// <summary/>
        HttpClient GetProxiedHttpClient(string proxyUrl);

    }
}
