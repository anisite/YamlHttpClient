using System;
using System.Net.Http;

namespace YamlHttpClient.Interfaces
{
    public interface IYamlHttpClient : IDisposable
    {
        HttpClient HttpClient { get; }

        HttpMessageHandler HttpMessageHandler { get; }

        string BaseUrl { get; set; }
        bool IsDisposed { get; }
    }
}
