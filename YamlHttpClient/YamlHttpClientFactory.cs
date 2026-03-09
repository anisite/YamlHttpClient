using HandlebarsDotNet;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YamlHttpClient.Exceptions;
using YamlHttpClient.Factory;
using YamlHttpClient.Settings;
using YamlHttpClient.Utils;

namespace YamlHttpClient
{
    /// <summary>
    /// Yaml config based HttpClient
    /// </summary>
    public partial class YamlHttpClientFactory : YamlHttpClientFactoryBase, IYamlHttpClientAccessor
    {
        private string _uniqueId => HttpClientSettings.Url;
        private readonly IContentHandler _contentHandler;

        /// <summary>
        /// Config
        /// </summary>
        public HttpClientSettings HttpClientSettings { get; set; }

        /// <summary>
        /// Handlebars
        /// </summary>
        public IHandlebars HandlebarsProvider { get; set; }

        private static readonly MemoryCache _httpResponseCache = new MemoryCache(new MemoryCacheOptions());

#if !NET6_0_OR_GREATER
        // Générateur Thread-Safe ultra performant pour .NET 3.1 et .NET 5.0
        private static readonly System.Threading.ThreadLocal<Random> _threadLocalRandom =
            new System.Threading.ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));
#endif

        public YamlHttpClientFactory()
        {
            HandlebarsProvider = CreateDefaultHandleBars();
        }

        /// <summary>
        /// 
        /// </summary>
        public YamlHttpClientFactory(HttpClientSettings httpClientSettings, IHandlebars? handlebars = null)
        {
            HttpClientSettings = httpClientSettings;
            HandlebarsProvider = handlebars ?? CreateDefaultHandleBars();
            _contentHandler = new ContentHandler(HandlebarsProvider);
        }

        /// <summary>
        /// Create a new instance of YamlHttpClientFactory
        /// </summary>
        /// <param name="httpClientSettings"></param>
        /// <param name="handlebars"></param>
        /// <param name="defaultClientTimeout"></param>
        public YamlHttpClientFactory(HttpClientSettings httpClientSettings,
                                     TimeSpan defaultClientTimeout,
                                     IHandlebars? handlebars = null) : base(defaultClientTimeout)
        {
            HttpClientSettings = httpClientSettings;
            HandlebarsProvider = handlebars ?? CreateDefaultHandleBars();
            _contentHandler = new ContentHandler(HandlebarsProvider);
        }

        private static readonly Lazy<IHandlebars> _defaultHandlebars = new Lazy<IHandlebars>(() =>
        {
            var hb = Handlebars.Create();
            hb.AddJsonHelper();
            hb.AddBase64();
            hb.AddIfCond(false);
            return hb;
        });

        /// <summary>
        /// </summary>
        public static IHandlebars CreateDefaultHandleBars()
        {
            // Retourne toujours la même instance partagée !
            return _defaultHandlebars.Value;
        }

        /// <summary>
        /// </summary>
        public static IHandlebars CreateEmptyHandleBars()
        {
            IHandlebars hb;
            hb = Handlebars.Create();

            return hb;
        }

        /// <summary>
        /// Cache key
        /// </summary>
        /// <returns></returns>
        protected override string GetCacheKey()
        {
            return _uniqueId;
        }

        /// <summary>
        /// Build request message
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public HttpRequestMessage BuildRequestMessage(dynamic data)
        {
            try
            {
                var msg = new HttpRequestMessage(new HttpMethod(HttpClientSettings.Method),
                                                            SS(HttpClientSettings.Url, data));

                msg.Content = (_contentHandler ?? new ContentHandler(HandlebarsProvider)).Content(data, HttpClientSettings.Content);

                // Check If Basic authentication 
                if (HttpClientSettings.AuthBasic is { })
                {
                    var basicAuth = Encoding.ASCII.GetBytes(HttpClientSettings.AuthBasic);
                    msg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(basicAuth));
                }

                // Adding all headers
                if (HttpClientSettings.Headers is { })
                {
                    foreach (var item in HttpClientSettings.Headers)
                    {
                        msg.Headers.TryAddWithoutValidation(item.Key, SS(item.Value, data));
                    }
                }

                return msg;
            }
            catch (UriFormatException ex)
            {
                throw new YamlHttpClientException($"Invalid URI : '{SS(HttpClientSettings.Url, data)}'", ex);
            }
        }

        /// <summary>
        /// Send http request to server with parameters from config
        /// </summary>
        /// <param name="httpRequestMessage"></param>
        /// <returns></returns>
        public virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage)
        {
            return SendAsyncCore(httpRequestMessage, CancellationToken.None);
        }

        /// <summary>
        /// Send http request to server with parameters from config
        /// </summary>
        /// <param name="httpRequestMessage"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
        {
            return SendAsyncCore(httpRequestMessage, cancellationToken);
        }

        /// <summary>
        /// Send http request to server with parameters from config
        /// </summary>
        /// <param name="requestFactory"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken = default)
        {
            var cacheSettings = HttpClientSettings.Cache;
            string? cacheKey = null;

            // ==========================================
            // 1. VÉRIFICATION DU CACHE AVANT L'APPEL
            // ==========================================
            if (cacheSettings?.Enabled == true)
            {
                // On génère le message juste pour lire son URL et son Body compilé
                using var msgForCache = requestFactory();
                string url = msgForCache.RequestUri?.ToString() ?? string.Empty;

                // On lit le body compilé par Handlebars
                string bodyStr = msgForCache.Content != null ? await msgForCache.Content.ReadAsStringAsync() : string.Empty;

                // Clé unique : Méthode + URL + Hash du Body. 
                // GetHashCode() est ultra rapide et parfait pour la RAM.
                cacheKey = $"CACHE_{HttpClientSettings.Method}_{url}_{bodyStr.GetHashCode()}";

                // Si on a un "Cache Hit"
                if (_httpResponseCache.TryGetValue(cacheKey, out CachedResponse cachedRes))
                {
                    // On reconstruit un HttpResponseMessage "frais" à partir des bytes en mémoire
                    var cachedHttpResponse = new HttpResponseMessage(cachedRes.StatusCode)
                    {
                        Content = new ByteArrayContent(cachedRes.ContentBytes)
                    };

                    if (!string.IsNullOrEmpty(cachedRes.ContentType))
                    {
                        cachedHttpResponse.Content.Headers.TryAddWithoutValidation("Content-Type", cachedRes.ContentType);
                    }

                    return cachedHttpResponse; // Sortie immédiate ! Zéro appel réseau.
                }
            }

            // ==========================================
            // 2. LOGIQUE D'APPEL RÉSEAU (AVEC RETRY)
            // ==========================================
            var retrySettings = HttpClientSettings.Retry;
            int maxAttempts = (retrySettings?.MaxRetries ?? 0) + 1;
            int delayMs = retrySettings?.DelayMilliseconds ?? 0;
            var codesToRetry = retrySettings?.RetryOnStatusCodes;

            Exception? lastException = null;
            HttpResponseMessage? finalResponse = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using var msg = requestFactory();

                try
                {
                    finalResponse = await SendAsyncCore(msg, cancellationToken);

                    if (attempt < maxAttempts && codesToRetry != null && codesToRetry.Contains((int)finalResponse.StatusCode))
                    {
                        finalResponse.Dispose();
                        await Task.Delay(delayMs, cancellationToken);
                        continue;
                    }

                    break; // Succès ou code d'erreur qu'on ne veut pas réessayer
                }
                catch (Exception ex) when (attempt < maxAttempts && IsRetryableException(ex, cancellationToken))
                {
                    lastException = ex;
                    await Task.Delay(delayMs, cancellationToken);
                }
            }

            if (finalResponse == null)
            {
                if (lastException != null)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(lastException).Throw();
                throw new InvalidOperationException("La boucle de réessai a échoué.");
            }

            // ==========================================
            // 3. SAUVEGARDE DANS LE CACHE (SI ACTIVÉ)
            // ==========================================
            if (cacheSettings?.Enabled == true && finalResponse.IsSuccessStatusCode && cacheKey != null)
            {
                // ⚠️ ATTENTION : Lire le contenu consomme et détruit le Stream réseau !
                byte[] bytes = await finalResponse.Content.ReadAsByteArrayAsync();
                string? contentType = finalResponse.Content.Headers.ContentType?.ToString();

                // On sauvegarde notre objet "sûr" dans la RAM
                _httpResponseCache.Set(cacheKey, new CachedResponse
                {
                    StatusCode = finalResponse.StatusCode,
                    ContentBytes = bytes,
                    ContentType = contentType
                }, TimeSpan.FromSeconds(cacheSettings.TtlSeconds));

                // 🚀 MAGIE : On remplace le flux réseau détruit par un flux mémoire frais
                // pour que ton `HandleAsync` ou ton utilisateur puisse quand même lire la réponse.
                finalResponse.Content = new ByteArrayContent(bytes);
                if (!string.IsNullOrEmpty(contentType))
                {
                    finalResponse.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
                }
            }

            return finalResponse;
        }

        private static bool IsRetryableException(Exception ex, CancellationToken ct)
        {
            if (ex is HttpRequestException) return true;
            if (ex is TaskCanceledException && !ct.IsCancellationRequested) return true;
            return false;
        }

        private async Task<HttpResponseMessage> SendAsyncCore(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
        {
            // ==========================================
            // 🐒 CHAOS INJECTION (Handles ALL requests, regardless of their origin)
            // ==========================================
            var chaos = HttpClientSettings.Chaos;
            if (chaos?.Enabled == true)
            {

#if NET6_0_OR_GREATER
                int chance = Random.Shared.Next(1, 101);
#else
                int chance = _threadLocalRandom.Value!.Next(1, 101);
#endif

                if (chance <= chaos.InjectionRatePercentage)
                {
                    // 1. Forced delay
                    if (chaos.DelayMilliseconds.HasValue)
                    {
                        await Task.Delay(chaos.DelayMilliseconds.Value, cancellationToken);
                    }

                    // 2. Violent network failure (Timeout, DNS lost, etc.)
                    if (chaos.SimulateNetworkException)
                    {
                        throw new HttpRequestException("🐒 CHAOS MONKEY: Pure network failure simulation.");
                    }

                    // 3. Bad HTTP status code (503, 500, etc.)
                    if (chaos.SimulateStatusCode.HasValue)
                    {
                        return new HttpResponseMessage((System.Net.HttpStatusCode)chaos.SimulateStatusCode.Value)
                        {
                            RequestMessage = httpRequestMessage, // Keep the reference for debugging purposes
                            Content = new StringContent("🐒 CHAOS MONKEY: Error response simulated by YAML configuration.")
                        };
                    }
                }
            }

            // ==========================================
            // NORMAL CALL (If the monkey hasn't struck)
            // ==========================================
            var client = GetHttpClient();

            try
            {
                return await client.SendAsync(httpRequestMessage, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                // Existing URI exception handling
                throw new YamlHttpClientException($"Invalid URI: '{SS(HttpClientSettings.Url, httpRequestMessage.RequestUri)}'", ex);
            }
        }

        /// <summary>
        /// Auto call a web url with settings from config and any data to work with.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> AutoCallAsync(dynamic data)
        {
            return AutoCallAsync(data, CancellationToken.None);
        }

        /// <summary>
        /// Auto call a web url with settings from config and any data to work with.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> AutoCallAsync(dynamic data, CancellationToken cancellationToken)
        {
            return SendAsync(() => BuildRequestMessage(data), cancellationToken);
        }


        /// <summary>
        /// Check response from check_response settings in yaml config
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public Task CheckResponseAsync(HttpResponseMessage response)
        {
            return CheckResponseAsync(response, CancellationToken.None);
        }

        /// <summary>
        /// Check response from check_response settings in yaml config
        /// </summary>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task CheckResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var checkSettings = HttpClientSettings.CheckResponse;

            if (checkSettings == null) return;

            bool hasContainsAny = checkSettings.ThrowExceptionIfBodyContainsAny?.Any() == true;
            bool hasNotContainsAll = checkSettings.ThrowExceptionIfBodyNotContainAll?.Any() == true;

            if (!hasContainsAny && !hasNotContainsAll) return;

#if NETCOREAPP3_1
            string body = await response.Content.ReadAsStringAsync();
#else
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
#endif
            if (hasContainsAny)
            {
                foreach (var item in checkSettings.ThrowExceptionIfBodyContainsAny!)
                {
                    if (body.Contains(item, StringComparison.Ordinal))
                    {
                        throw new ThrowExceptionIfBodyContainsAny(item);
                    }
                }
            }

            if (hasNotContainsAll)
            {
                foreach (var item in checkSettings.ThrowExceptionIfBodyNotContainAll!)
                {
                    if (!body.Contains(item, StringComparison.Ordinal))
                    {
                        throw new ThrowExceptionIfBodyNotContainAll(item);
                    }
                }
            }
        }

        /// <summary>
        /// Message handler
        /// </summary>
        /// <param name="proxyUrl"></param>
        /// <returns></returns>
        protected override HttpMessageHandler CreateMessageHandler(string? proxyUrl = null)
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                AllowAutoRedirect = true
            };

            if (HttpClientSettings.UseDefaultCredentials)
            {
                handler.Credentials = System.Net.CredentialCache.DefaultCredentials;
            }

            if (!string.IsNullOrEmpty(proxyUrl))
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(proxyUrl);

                if (HttpClientSettings.UseDefaultCredentials)
                {
                    handler.DefaultProxyCredentials = System.Net.CredentialCache.DefaultCredentials;
                }
            }

            return handler;
        }

        /// <summary>
        /// Sustitute data with Stubble
        /// </summary>
        /// <param name="value">Value with placeholders {{example}}</param>
        /// <param name="data">Any data object to search from</param>
        /// <returns></returns>
        private string SS(string value, object data)
        {
            var comp = HandlebarsProvider.CompileWithCache(value);
            return comp(data);
        }
    }
}
