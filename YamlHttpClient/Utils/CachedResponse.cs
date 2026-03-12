using System;
using System.Net;

namespace YamlHttpClient
{
    public class CachedResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public byte[] ContentBytes { get; set; } = Array.Empty<byte>();
        public string? ContentType { get; set; }
    }

}
