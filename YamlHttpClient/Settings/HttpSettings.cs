using System;
using System.Net.Http;

namespace YamlHttpClient.Settings
{
    public class HttpRunTimeSettings : IDisposable
    {
        public HttpRunTimeSettings()
        {
            SetCurrentTest(this);
        }

        public void Dispose()
        {
            SetCurrentTest(null);
        }

        public static HttpRunTimeSettings Current => GetCurrentTest();

        public TimeSpan? Timeout { get; set; }
        public TimeSpan? ConnectionLeaseTimeout { get; set; }

        public HttpMessageHandler HttpMessageHandler { get; set; }

        private static readonly System.Threading.AsyncLocal<HttpRunTimeSettings> _test = new System.Threading.AsyncLocal<HttpRunTimeSettings>();
        private static void SetCurrentTest(HttpRunTimeSettings test) => _test.Value = test;
        private static HttpRunTimeSettings GetCurrentTest() => _test.Value;

    }
}
