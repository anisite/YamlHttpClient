using System;
using System.Net.Http;

namespace YamlHttpClient.Settings
{
    /// <summary />
    public class HttpRunTimeSettings : IDisposable
    {
        /// <summary />
        public HttpRunTimeSettings()
        {
            SetCurrentTest(this);
        }

        /// <summary />
        public void Dispose()
        {
            SetCurrentTest(null);
        }

        /// <summary />
        public static HttpRunTimeSettings Current => GetCurrentTest();

        /// <summary />
        public TimeSpan? Timeout { get; set; }
        /// <summary />
        public TimeSpan? ConnectionLeaseTimeout { get; set; }
        /// <summary />
        public HttpMessageHandler HttpMessageHandler { get; set; } = default!;

        private static readonly System.Threading.AsyncLocal<HttpRunTimeSettings> _test = new System.Threading.AsyncLocal<HttpRunTimeSettings>();
        private static void SetCurrentTest(HttpRunTimeSettings? test) => _test.Value = test!;
        private static HttpRunTimeSettings GetCurrentTest() => _test.Value!;

    }
}
