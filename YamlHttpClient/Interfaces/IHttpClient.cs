using System;
using System.Net.Http;

namespace YamlHttpClient.Interfaces
{
    /// <summary />
    public interface IYamlHttpClient : IDisposable
    {
        /// <summary />
        HttpClient HttpClient { get; }
        /// <summary />
        HttpMessageHandler HttpMessageHandler { get; }
        /// <summary />
        string? BaseUrl { get; set; }
        /// <summary />
        bool IsDisposed { get; }
    }
}
