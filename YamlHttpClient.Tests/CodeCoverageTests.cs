using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using YamlHttpClient.Exceptions;
using YamlHttpClient.Settings;

namespace YamlHttpClient.Tests
{
    // =====================================================================
    // HELPERS
    // =====================================================================

    /// <summary>
    /// DelegatingHandler injectable pour simuler des réponses HTTP sans serveur réel.
    /// </summary>
    internal class FakeHttpHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new Queue<HttpResponseMessage>();

        public FakeHttpHandler(params HttpResponseMessage[] responses)
        {
            foreach (var r in responses)
                _responses.Enqueue(r);
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (_responses.Count == 0)
                throw new InvalidOperationException("FakeHttpHandler: no more responses queued.");
            return Task.FromResult(_responses.Dequeue());
        }
    }

    /// <summary>
    /// Handler qui lève une HttpRequestException au premier appel, puis retourne des réponses normales.
    /// </summary>
    internal class ThrowOnceFakeHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new Queue<HttpResponseMessage>();
        private bool _hasThrown = false;

        public int CallCount { get; private set; }

        public ThrowOnceFakeHandler(params HttpResponseMessage[] responses)
        {
            foreach (var r in responses)
                _responses.Enqueue(r);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (!_hasThrown)
            {
                _hasThrown = true;
                throw new HttpRequestException("Simulated network failure");
            }
            if (_responses.Count == 0)
                throw new InvalidOperationException("ThrowOnceFakeHandler: no more responses queued.");
            return Task.FromResult(_responses.Dequeue());
        }
    }

    /// <summary>
    /// Sous-classe de YamlHttpClientFactory qui injecte un handler de test.
    /// </summary>
    internal class TestableYamlHttpClientFactory : YamlHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public TestableYamlHttpClientFactory(HttpClientSettings settings, HttpMessageHandler handler)
            : base(settings)
        {
            _handler = handler;
        }

        public TestableYamlHttpClientFactory(HttpClientSettings settings, TimeSpan timeout, HttpMessageHandler handler)
            : base(settings, timeout)
        {
            _handler = handler;
        }

        protected override HttpMessageHandler CreateMessageHandler(string? proxyUrl = null) => _handler;
    }

    // =====================================================================
    // TESTS CACHE
    // =====================================================================

    [TestClass]
    public class CacheTests
    {
        private static HttpClientSettings MakeCachedSettings(int ttl = 60) => new HttpClientSettings
        {
            Method = "GET",
            Url = $"http://localhost/cache-test-{Guid.NewGuid()}",  // URL unique par test pour isoler le cache statique
            Cache = new CacheSettings { Enabled = true, TtlSeconds = ttl }
        };

        [TestMethod]
        public async Task Cache_SecondCall_DoesNotHitNetwork()
        {
            var fakeHandler = new FakeHttpHandler(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("response-body") },
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("should-not-be-called") }
            );

            var factory = new TestableYamlHttpClientFactory(MakeCachedSettings(), fakeHandler);

            var r1 = await factory.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, factory.HttpClientSettings.Url));
            var r2 = await factory.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, factory.HttpClientSettings.Url));

            Assert.AreEqual("response-body", await r1.Content.ReadAsStringAsync());
            Assert.AreEqual("response-body", await r2.Content.ReadAsStringAsync());
            Assert.AreEqual(1, fakeHandler.CallCount, "Le réseau ne doit être appelé qu'une seule fois grâce au cache.");
        }

        [TestMethod]
        public async Task Cache_Miss_OnDifferentBody_HitsNetworkTwice()
        {
            var settings = MakeCachedSettings();
            var fakeHandler = new FakeHttpHandler(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("resp-1") },
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("resp-2") }
            );

            var factory = new TestableYamlHttpClientFactory(settings, fakeHandler);

            // Deux corps différents => deux clés de cache différentes
            var r1 = await factory.SendAsync(() => new HttpRequestMessage(HttpMethod.Post, settings.Url)
            {
                Content = new StringContent("body-A")
            });
            var r2 = await factory.SendAsync(() => new HttpRequestMessage(HttpMethod.Post, settings.Url)
            {
                Content = new StringContent("body-B")
            });

            Assert.AreEqual(2, fakeHandler.CallCount, "Des corps différents => pas de cache hit.");
        }

        [TestMethod]
        public async Task Cache_Disabled_AlwaysHitsNetwork()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = $"http://localhost/no-cache-{Guid.NewGuid()}",
                Cache = new CacheSettings { Enabled = false }
            };

            var fakeHandler = new FakeHttpHandler(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("r1") },
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("r2") }
            );

            var factory = new TestableYamlHttpClientFactory(settings, fakeHandler);

            await factory.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, settings.Url));
            await factory.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, settings.Url));

            Assert.AreEqual(2, fakeHandler.CallCount, "Sans cache, chaque appel doit atteindre le réseau.");
        }

        [TestMethod]
        public async Task Cache_NoCache_Setting_AlwaysHitsNetwork()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = $"http://localhost/null-cache-{Guid.NewGuid()}"
                // Cache = null intentionnellement
            };

            var fakeHandler = new FakeHttpHandler(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("r1") },
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("r2") }
            );

            var factory = new TestableYamlHttpClientFactory(settings, fakeHandler);

            await factory.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, settings.Url));
            await factory.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, settings.Url));

            Assert.AreEqual(2, fakeHandler.CallCount);
        }

        [TestMethod]
        public async Task Cache_ErrorResponse_IsNotCached()
        {
            var settings = MakeCachedSettings();
            var fakeHandler = new FakeHttpHandler(
                new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("error") },
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") }
            );

            var factory = new TestableYamlHttpClientFactory(settings, fakeHandler);

            var r1 = await factory.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, settings.Url));
            var r2 = await factory.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, settings.Url));

            Assert.AreEqual(HttpStatusCode.InternalServerError, r1.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, r2.StatusCode);
            Assert.AreEqual(2, fakeHandler.CallCount, "Une réponse en erreur ne doit pas être mise en cache.");
        }
    }

    // =====================================================================
    // TESTS RETRY
    // =====================================================================

    [TestClass]
    public class RetryTests
    {
        [TestMethod]
        public async Task Retry_OnStatusCode_RetriesCorrectNumberOfTimes()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = $"http://localhost/retry-{Guid.NewGuid()}",
                Retry = new RetrySettings
                {
                    MaxRetries = 2,
                    DelayMilliseconds = 0,
                    RetryOnStatusCodes = new List<int> { 503 }
                }
            };

            var fakeHandler = new FakeHttpHandler(
                new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
                new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("success") }
            );

            var factory = new TestableYamlHttpClientFactory(settings, fakeHandler);
            var response = await factory.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, settings.Url));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(3, fakeHandler.CallCount, "Doit faire 1 appel initial + 2 retries.");
        }

        [TestMethod]
        public async Task Retry_StopsAfterMaxRetries_WhenStatusCodeAlwaysFails()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = $"http://localhost/retry-max-{Guid.NewGuid()}",
                Retry = new RetrySettings
                {
                    MaxRetries = 1,
                    DelayMilliseconds = 0,
                    RetryOnStatusCodes = new List<int> { 500 }
                }
            };

            var fakeHandler = new FakeHttpHandler(
                new HttpResponseMessage(HttpStatusCode.InternalServerError),
                new HttpResponseMessage(HttpStatusCode.InternalServerError)
            );

            var factory = new TestableYamlHttpClientFactory(settings, fakeHandler);
            var response = await factory.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, settings.Url));

            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.AreEqual(2, fakeHandler.CallCount, "1 appel + 1 retry max.");
        }

        [TestMethod]
        public async Task Retry_NoRetry_WhenStatusCodeNotInList()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = $"http://localhost/retry-no-{Guid.NewGuid()}",
                Retry = new RetrySettings
                {
                    MaxRetries = 3,
                    DelayMilliseconds = 0,
                    RetryOnStatusCodes = new List<int> { 503 } // On ne liste pas le 500
                }
            };

            var fakeHandler = new FakeHttpHandler(
                new HttpResponseMessage(HttpStatusCode.InternalServerError)
            );

            var factory = new TestableYamlHttpClientFactory(settings, fakeHandler);
            var response = await factory.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, settings.Url));

            Assert.AreEqual(1, fakeHandler.CallCount, "Pas de retry si le code HTTP ne figure pas dans la liste.");
        }

        [TestMethod]
        public async Task Retry_OnNetworkException_RetriesAndSucceeds()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = $"http://localhost/retry-exc-{Guid.NewGuid()}",
                Retry = new RetrySettings { MaxRetries = 1, DelayMilliseconds = 0 }
            };

            // Handler qui lève une exception au 1er appel, puis retourne 200 au 2e
            var throwThenSucceedHandler = new ThrowOnceFakeHandler(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") }
            );

            var factory = new TestableYamlHttpClientFactory(settings, throwThenSucceedHandler);
            var response = await factory.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, settings.Url));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(2, throwThenSucceedHandler.CallCount, "1 échec réseau + 1 retry réussi.");
        }

        [TestMethod]
        public async Task Retry_NullRetrySettings_DoesNotRetry()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = $"http://localhost/no-retry-{Guid.NewGuid()}"
                // Retry = null
            };

            var fakeHandler = new FakeHttpHandler(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") }
            );

            var factory = new TestableYamlHttpClientFactory(settings, fakeHandler);
            var response = await factory.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, settings.Url));

            Assert.AreEqual(1, fakeHandler.CallCount);
        }
    }

    // =====================================================================
    // TESTS CHAOS MONKEY
    // =====================================================================

    [TestClass]
    public class ChaosTests
    {
        [TestMethod]
        public async Task Chaos_SimulateStatusCode_ReturnsExpectedCode()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = $"http://localhost/chaos-{Guid.NewGuid()}",
                Chaos = new ChaosSettings
                {
                    Enabled = true,
                    InjectionRatePercentage = 100,
                    SimulateStatusCode = 503
                }
            };

            var fakeHandler = new FakeHttpHandler(
                new HttpResponseMessage(HttpStatusCode.OK)  // Ne devrait jamais être atteint
            );

            var factory = new TestableYamlHttpClientFactory(settings, fakeHandler);
            var request = new HttpRequestMessage(HttpMethod.Get, settings.Url);
            var response = await factory.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.AreEqual(0, fakeHandler.CallCount, "Le chaos à 100% doit court-circuiter le réseau.");
        }

        [TestMethod]
        public async Task Chaos_SimulateNetworkException_ThrowsHttpRequestException()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = $"http://localhost/chaos-exc-{Guid.NewGuid()}",
                Chaos = new ChaosSettings
                {
                    Enabled = true,
                    InjectionRatePercentage = 100,
                    SimulateNetworkException = true
                }
            };

            var fakeHandler = new FakeHttpHandler();
            var factory = new TestableYamlHttpClientFactory(settings, fakeHandler);

            await Assert.ThrowsExceptionAsync<HttpRequestException>(async () =>
                await factory.SendAsync(new HttpRequestMessage(HttpMethod.Get, settings.Url))
            );
        }

        [TestMethod]
        public async Task Chaos_WithDelay_DoesNotThrow()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = $"http://localhost/chaos-delay-{Guid.NewGuid()}",
                Chaos = new ChaosSettings
                {
                    Enabled = true,
                    InjectionRatePercentage = 0,  // Aucune injection, juste le délai
                    DelayMilliseconds = 1
                }
            };

            var fakeHandler = new FakeHttpHandler(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") }
            );

            var factory = new TestableYamlHttpClientFactory(settings, fakeHandler);
            var response = await factory.SendAsync(new HttpRequestMessage(HttpMethod.Get, settings.Url));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Chaos_Disabled_PassesThrough()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = $"http://localhost/chaos-off-{Guid.NewGuid()}",
                Chaos = new ChaosSettings { Enabled = false, InjectionRatePercentage = 100, SimulateStatusCode = 500 }
            };

            var fakeHandler = new FakeHttpHandler(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("real-response") }
            );

            var factory = new TestableYamlHttpClientFactory(settings, fakeHandler);
            var response = await factory.SendAsync(new HttpRequestMessage(HttpMethod.Get, settings.Url));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(1, fakeHandler.CallCount);
        }
    }

    // =====================================================================
    // TESTS CHECK RESPONSE
    // =====================================================================

    [TestClass]
    public class CheckResponseTests
    {
        [TestMethod]
        [ExpectedException(typeof(ThrowExceptionIfBodyContainsAny))]
        public async Task CheckResponse_ThrowsWhenBodyContainsForbiddenWord()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = "http://localhost/check",
                CheckResponse = new CheckResponse
                {
                    ThrowExceptionIfBodyContainsAny = new List<string> { "error" }
                }
            };

            var factory = new YamlHttpClientFactory(settings);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\": \"error\", \"code\": 42}")
            };

            await factory.CheckResponseAsync(response);
        }

        [TestMethod]
        public async Task CheckResponse_NoThrow_WhenBodyIsClean()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = "http://localhost/check",
                CheckResponse = new CheckResponse
                {
                    ThrowExceptionIfBodyContainsAny = new List<string> { "error" },
                    ThrowExceptionIfBodyNotContainAll = new List<string> { "success" }
                }
            };

            var factory = new YamlHttpClientFactory(settings);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\": \"success\"}")
            };

            // Ne doit pas lever d'exception
            await factory.CheckResponseAsync(response);
        }

        [TestMethod]
        [ExpectedException(typeof(ThrowExceptionIfBodyNotContainAll))]
        public async Task CheckResponse_ThrowsWhenRequiredWordMissing()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = "http://localhost/check",
                CheckResponse = new CheckResponse
                {
                    ThrowExceptionIfBodyNotContainAll = new List<string> { "expected_token" }
                }
            };

            var factory = new YamlHttpClientFactory(settings);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\": \"ok\"}")
            };

            await factory.CheckResponseAsync(response);
        }

        [TestMethod]
        public async Task CheckResponse_NullSettings_DoesNothing()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = "http://localhost/check"
                // CheckResponse = null
            };

            var factory = new YamlHttpClientFactory(settings);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("anything")
            };

            await factory.CheckResponseAsync(response, CancellationToken.None);
        }

        [TestMethod]
        public async Task CheckResponse_EmptyLists_DoesNothing()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = "http://localhost/check",
                CheckResponse = new CheckResponse
                {
                    ThrowExceptionIfBodyContainsAny = new List<string>(),
                    ThrowExceptionIfBodyNotContainAll = new List<string>()
                }
            };

            var factory = new YamlHttpClientFactory(settings);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("anything")
            };

            await factory.CheckResponseAsync(response);
        }
    }

    // =====================================================================
    // TESTS BUILD REQUEST MESSAGE
    // =====================================================================

    [TestClass]
    public class BuildRequestMessageTests
    {
        [TestMethod]
        public void BuildRequestMessage_SetsMethodAndUrl()
        {
            var settings = new HttpClientSettings
            {
                Method = "POST",
                Url = "http://localhost/api/test",
                Content = new ContentSettings { JsonContent = "{\"key\":\"{{value}}\"}" }
            };

            var factory = new YamlHttpClientFactory(settings);
            var data = new { value = "hello" };

            var request = factory.BuildRequestMessage(data);

            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("http://localhost/api/test", request.RequestUri!.ToString());
        }

        [TestMethod]
        public void BuildRequestMessage_TemplatesUrlCorrectly()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = "http://localhost/api/{{id}}",
            };

            var factory = new YamlHttpClientFactory(settings);
            var request = factory.BuildRequestMessage(new { id = "42" });

            Assert.AreEqual("http://localhost/api/42", request.RequestUri!.ToString());
        }

        [TestMethod]
        public void BuildRequestMessage_AddsHeaders()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = "http://localhost/api",
                Headers = new Dictionary<string, string>
                {
                    { "X-Custom-Header", "{{token}}" },
                    { "Accept", "application/json" }
                }
            };

            var factory = new YamlHttpClientFactory(settings);
            var request = factory.BuildRequestMessage(new { token = "abc123" });

            Assert.IsTrue(request.Headers.Contains("X-Custom-Header"));
            Assert.IsTrue(request.Headers.Contains("Accept"));
        }

        [TestMethod]
        public void BuildRequestMessage_AddsBasicAuthHeader()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = "http://localhost/secure",
                AuthBasic = "user:password"
            };

            var factory = new YamlHttpClientFactory(settings);
            var request = factory.BuildRequestMessage(new { });

            Assert.IsNotNull(request.Headers.Authorization);
            Assert.AreEqual("Basic", request.Headers.Authorization!.Scheme);
        }

        [TestMethod]
        [ExpectedException(typeof(YamlHttpClientException))]
        public void BuildRequestMessage_ThrowsOnInvalidUri()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = "https:///invalid"
            };

            var factory = new YamlHttpClientFactory(settings);
            factory.BuildRequestMessage(new { });
        }

        [TestMethod]
        public void BuildRequestMessage_StringContent()
        {
            var settings = new HttpClientSettings
            {
                Method = "POST",
                Url = "http://localhost/api",
                Content = new ContentSettings { StringContent = "raw body {{val}}" }
            };

            var factory = new YamlHttpClientFactory(settings);
            var request = factory.BuildRequestMessage(new { val = "test" });

            Assert.IsNotNull(request.Content);
        }
    }

    // =====================================================================
    // TESTS AUTO CALL ASYNC
    // =====================================================================

    [TestClass]
    public class AutoCallAsyncTests
    {
        [TestMethod]
        public async Task AutoCallAsync_CallsSendAsync_WithBuiltRequest()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = $"http://localhost/auto-{Guid.NewGuid()}"
            };

            var fakeHandler = new FakeHttpHandler(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("auto-result") }
            );

            var factory = new TestableYamlHttpClientFactory(settings, fakeHandler);
            var response = await factory.AutoCallAsync(new { });

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("auto-result", await response.Content.ReadAsStringAsync());
        }

        [TestMethod]
        public async Task AutoCallAsync_WithCancellationToken_Works()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = $"http://localhost/auto-ct-{Guid.NewGuid()}"
            };

            var fakeHandler = new FakeHttpHandler(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") }
            );

            var factory = new TestableYamlHttpClientFactory(settings, fakeHandler);
            using var cts = new CancellationTokenSource();
            var response = await factory.AutoCallAsync(new { }, cts.Token);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }

    // =====================================================================
    // TESTS HANDLEBARS FACTORIES
    // =====================================================================

    [TestClass]
    public class HandleBarsFactoryTests
    {
        [TestMethod]
        public void CreateDefaultHandleBars_ReturnsSameInstance()
        {
            var hb1 = YamlHttpClientFactory.CreateDefaultHandleBars();
            var hb2 = YamlHttpClientFactory.CreateDefaultHandleBars();

            Assert.AreSame(hb1, hb2, "CreateDefaultHandleBars doit retourner la même instance (Lazy singleton).");
        }

        [TestMethod]
        public void CreateEmptyHandleBars_ReturnsDifferentInstances()
        {
            var hb1 = YamlHttpClientFactory.CreateEmptyHandleBars();
            var hb2 = YamlHttpClientFactory.CreateEmptyHandleBars();

            Assert.AreNotSame(hb1, hb2, "CreateEmptyHandleBars doit créer une nouvelle instance à chaque appel.");
        }

        [TestMethod]
        public void CreateEmptyHandleBars_IsNotNull()
        {
            var hb = YamlHttpClientFactory.CreateEmptyHandleBars();
            Assert.IsNotNull(hb);
        }

        [TestMethod]
        public void DefaultConstructor_SetsHandlebarsProvider()
        {
            var factory = new YamlHttpClientFactory();
            Assert.IsNotNull(factory.HandlebarsProvider);
        }
    }

    // =====================================================================
    // TESTS HTTP CLIENT FACTORY BASE
    // =====================================================================

    [TestClass]
    public class HttpClientFactoryBaseTests
    {
        [TestMethod]
        public void GetHttpClient_ReturnsSameInstance_ForSameUrl()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = $"http://localhost/same-url-{Guid.NewGuid()}"
            };

            var factory = new YamlHttpClientFactory(settings);

            var c1 = factory.GetHttpClient();
            var c2 = factory.GetHttpClient();

            Assert.AreSame(c1, c2, "Le même HttpClient doit être retourné pour la même URL (cache).");
        }

        [TestMethod]
        public void GetProxiedHttpClient_ThrowsOnNullProxy()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = "http://localhost/proxy"
            };

            var factory = new YamlHttpClientFactory(settings);

            Assert.ThrowsException<ArgumentNullException>(() => factory.GetProxiedHttpClient(null!));
        }

        [TestMethod]
        public void GetProxiedHttpClient_ReturnsSameInstanceForSameProxy()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = "http://localhost/proxy"
            };
            var factory = new YamlHttpClientFactory(settings);

            var client1 = factory.GetProxiedHttpClient("http://proxy:8080");
            var client2 = factory.GetProxiedHttpClient("http://proxy:8080");

            Assert.AreSame(client1, client2);
        }

        [TestMethod]
        public void GetProxiedHttpClient_ReturnsDifferentInstancesForDifferentProxies()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = "http://localhost/proxy"
            };
            var factory = new YamlHttpClientFactory(settings);

            var client1 = factory.GetProxiedHttpClient("http://proxy1:8080");
            var client2 = factory.GetProxiedHttpClient("http://proxy2:9090");

            Assert.AreNotSame(client1, client2);
        }

        [TestMethod]
        public void GetProxiedHttpClient_ReturnsDifferentInstanceFromNonProxiedClient()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = "http://localhost/proxy"
            };
            var factory = new YamlHttpClientFactory(settings);

            var proxiedClient = factory.GetProxiedHttpClient("http://proxy:8080");
            var normalClient = factory.GetHttpClient();

            Assert.AreNotSame(proxiedClient, normalClient);
        }

        [TestMethod]
        public void GetProxiedHttpClient_ReturnsHttpClientInstance()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = "http://localhost/proxy"
            };
            var factory = new YamlHttpClientFactory(settings);

            var client = factory.GetProxiedHttpClient("http://proxy:8080");

            Assert.IsNotNull(client);
            Assert.IsInstanceOfType(client, typeof(HttpClient));
        }

        [TestMethod]
        public void GetProxiedHttpClient_ThrowsOnEmptyProxy()
        {
            var settings = new HttpClientSettings
            {
                Method = "GET",
                Url = "http://localhost/proxy"
            };

            var factory = new YamlHttpClientFactory(settings);

            Assert.ThrowsException<ArgumentNullException>(() => factory.GetProxiedHttpClient(string.Empty));
        }

        [TestMethod]
        public void DefaultClientTimeout_IsSetCorrectly()
        {
            var timeout = TimeSpan.FromSeconds(42);
            var settings = new HttpClientSettings { Method = "GET", Url = "http://localhost" };
            var factory = new YamlHttpClientFactory(settings, timeout);

            Assert.AreEqual(timeout, factory.DefaultClientTimeout);
        }
    }

    // =====================================================================
    // TESTS SETTINGS
    // =====================================================================

    [TestClass]
    public class SettingsDefaultsTests
    {
        [TestMethod]
        public void CacheSettings_DefaultTtl_Is600()
        {
            var s = new CacheSettings();
            Assert.AreEqual(600, s.TtlSeconds);
            Assert.IsFalse(s.Enabled);
        }

        [TestMethod]
        public void RetrySettings_Defaults()
        {
            var s = new RetrySettings();
            Assert.AreEqual(3, s.MaxRetries);
            Assert.AreEqual(1000, s.DelayMilliseconds);
            Assert.IsNull(s.RetryOnStatusCodes);
        }

        [TestMethod]
        public void ChaosSettings_Defaults()
        {
            var s = new ChaosSettings();
            Assert.IsFalse(s.Enabled);
            Assert.AreEqual(33, s.InjectionRatePercentage);
            Assert.IsNull(s.DelayMilliseconds);
            Assert.IsNull(s.SimulateStatusCode);
            Assert.IsFalse(s.SimulateNetworkException);
        }
    }
}
