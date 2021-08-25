using System;
using System.Net.Http;

namespace YamlHttpClient.Factory
{
    public class HttpRunTimeSeetings : IDisposable
    {
        public HttpRunTimeSeetings()
        {
            SetCurrentTest(this);
        }

        public void Dispose()
        {
            SetCurrentTest(null);
        }

        public static HttpRunTimeSeetings Current => GetCurrentTest();

        public TimeSpan? Timeout { get; set; }
        public TimeSpan? ConnectionLeaseTimeout { get; set; }

        public HttpMessageHandler HttpMessageHandler { get; set; }

        private static readonly System.Threading.AsyncLocal<HttpRunTimeSeetings> _test = new System.Threading.AsyncLocal<HttpRunTimeSeetings>();
        private static void SetCurrentTest(HttpRunTimeSeetings test) => _test.Value = test;
        private static HttpRunTimeSeetings GetCurrentTest() => _test.Value;

    }
}
