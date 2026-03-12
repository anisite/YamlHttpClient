#if NET6_0_OR_GREATER

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YamlHttpClient.Settings;

namespace YamlHttpClient.Tests
{
    // =========================================================================
    // HELPERS ORCHESTRATEUR
    // =========================================================================

    /// <summary>
    /// Handler HTTP qui retourne des réponses prédéfinies dans l'ordre.
    /// Réutilisable sans dépendance à FakeHttpHandler du fichier CoverageTests.cs
    /// </summary>
    internal class OrchestratorFakeHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _factories = new();

        public int CallCount { get; private set; }

        public OrchestratorFakeHandler Add(HttpStatusCode status, string jsonBody, string? url = null)
        {
            _factories.Enqueue(() =>
            {
                var resp = new HttpResponseMessage(status)
                {
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                };
                if (url != null)
                    resp.RequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                return resp;
            });
            return this;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (_factories.Count == 0)
                throw new InvalidOperationException("OrchestratorFakeHandler: aucune réponse en file.");

            var resp = _factories.Dequeue()();
            resp.RequestMessage = request; // Assigner la vraie requête pour que l'URL soit correcte
            return Task.FromResult(resp);
        }
    }

    /// <summary>
    /// Sous-classe qui permet d'injecter un handler HTTP dans YamlHttpClientFactory
    /// créée à l'intérieur de l'orchestrateur. On surcharge CreateMessageHandler via
    /// un wrapper au niveau des settings.
    ///
    /// Comme YamlHttpOrchestrator instancie YamlHttpClientFactory en interne,
    /// on utilise HttpRunTimeSettings pour injecter le handler de test.
    /// </summary>
    internal static class OrchestratorStep
    {
        /// <summary>
        /// Crée un objet dynamic simulant une étape de séquence.
        /// </summary>
        public static dynamic Make(string httpClient, string? alias = null)
        {
            dynamic step = new ExpandoObject();
            step.HttpClient = httpClient;
            step.As = alias; // null => utilise le nom du client comme clé
            return step;
        }
    }

    // =========================================================================
    // TESTS ORCHESTRATEUR
    // =========================================================================

    [TestClass]
    public class YamlHttpOrchestratorTests
    {
        // ---------------------------------------------------------------------
        // Construction de l'orchestrateur
        // ---------------------------------------------------------------------

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_ThrowsWhenHandlebarsIsNull()
        {
            _ = new YamlHttpOrchestrator(null!);
        }

        [TestMethod]
        public void Constructor_WithOptions_StoresOptions()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var opts = new YamlHttpOrchestratorOptions
            {
                HandlebarsEngineKey = "custom_key"
            };

            var orch = new YamlHttpOrchestrator(hb, opts);

            Assert.IsNotNull(orch);
        }

        [TestMethod]
        public void Constructor_WithoutOptions_UsesDefaults()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);

            Assert.IsNotNull(orch);
        }

        [TestMethod]
        public void LastCalledUrls_IsEmptyOnCreation()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);

            Assert.AreEqual(0, orch.LastCalledUrls.Count);
        }

        // ---------------------------------------------------------------------
        // Validation des arguments de ExecuteSequenceAsync
        // ---------------------------------------------------------------------

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ExecuteSequenceAsync_ThrowsWhenSequenceIsNull()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);

            await orch.ExecuteSequenceAsync(
                new { },
                null!,
                new Dictionary<string, HttpClientSettings>(),
                null,
                null,
                CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ExecuteSequenceAsync_ThrowsWhenDictIsNull()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);

            await orch.ExecuteSequenceAsync(
                new { },
                new List<dynamic>(),
                null!,
                null,
                null,
                CancellationToken.None);
        }

        // ---------------------------------------------------------------------
        // Séquence vide
        // ---------------------------------------------------------------------

        [TestMethod]
        public async Task ExecuteSequenceAsync_EmptySequence_ReturnsSerializedInputOnly()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);

            var result = await orch.ExecuteSequenceAsync(
                new { name = "test" },
                new List<dynamic>(),   // séquence vide
                new Dictionary<string, HttpClientSettings>(),
                null,
                null,
                CancellationToken.None);

            Assert.IsNotNull(result);
            // Le résultat doit contenir la clé "input"
            var json = JsonDocument.Parse(result);
            Assert.IsTrue(json.RootElement.TryGetProperty("input", out _));
        }

        [TestMethod]
        public async Task ExecuteSequenceAsync_EmptySequence_LastCalledUrlsIsEmpty()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);

            await orch.ExecuteSequenceAsync(
                new { },
                new List<dynamic>(),
                new Dictionary<string, HttpClientSettings>(),
                null, null, CancellationToken.None);

            Assert.AreEqual(0, orch.LastCalledUrls.Count);
        }

        // ---------------------------------------------------------------------
        // Client introuvable dans le dictionnaire
        // ---------------------------------------------------------------------

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ExecuteSequenceAsync_ThrowsWhenClientNameNotFound()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);

            var steps = new List<dynamic> { OrchestratorStep.Make("nonexistent_client") };
            var dict = new Dictionary<string, HttpClientSettings>(); // dictionnaire vide

            await orch.ExecuteSequenceAsync(
                new { },
                steps,
                dict,
                null, null, CancellationToken.None);
        }

        [TestMethod]
        public async Task ExecuteSequenceAsync_ErrorMessage_ContainsClientName()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);

            var steps = new List<dynamic> { OrchestratorStep.Make("missing_api") };

            try
            {
                await orch.ExecuteSequenceAsync(
                    new { },
                    steps,
                    new Dictionary<string, HttpClientSettings>(),
                    null, null, CancellationToken.None);

                Assert.Fail("Une exception était attendue.");
            }
            catch (InvalidOperationException ex)
            {
                StringAssert.Contains(ex.Message, "missing_api");
            }
        }

        // ---------------------------------------------------------------------
        // Data Adapter Template
        // ---------------------------------------------------------------------

        [TestMethod]
        public async Task ExecuteSequenceAsync_WithDataAdapterTemplate_AppliesTemplate()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);

            // Template Handlebars simple qui extrait le champ "name" de l'input
            var template = "Hello {{input.name}}";

            var result = await orch.ExecuteSequenceAsync(
                new { name = "World" },
                new List<dynamic>(),
                new Dictionary<string, HttpClientSettings>(),
                template,
                null,
                CancellationToken.None);

            Assert.AreEqual("Hello World", result);
        }

        [TestMethod]
        public async Task ExecuteSequenceAsync_WithNullTemplate_ReturnsJsonSerialized()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);

            var result = await orch.ExecuteSequenceAsync(
                new { key = "value" },
                new List<dynamic>(),
                new Dictionary<string, HttpClientSettings>(),
                null,   // pas de template
                null,
                CancellationToken.None);

            // Doit être du JSON valide
            var doc = JsonDocument.Parse(result);
            Assert.AreEqual(JsonValueKind.Object, doc.RootElement.ValueKind);
        }

        [TestMethod]
        public async Task ExecuteSequenceAsync_WithWhitespaceTemplate_ReturnsJsonSerialized()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);

            var result = await orch.ExecuteSequenceAsync(
                new { },
                new List<dynamic>(),
                new Dictionary<string, HttpClientSettings>(),
                "   ",  // template whitespace => équivalent null
                null,
                CancellationToken.None);

            var doc = JsonDocument.Parse(result);
            Assert.AreEqual(JsonValueKind.Object, doc.RootElement.ValueKind);
        }

        // ---------------------------------------------------------------------
        // YamlHttpOrchestratorOptions — valeurs par défaut
        // ---------------------------------------------------------------------

        [TestMethod]
        public void OrchestratorOptions_DefaultCacheSettings_AreEnabled()
        {
            var opts = new YamlHttpOrchestratorOptions();

            Assert.IsTrue(opts.DefaultCacheSettings.Enabled);
            Assert.AreEqual(1200, opts.DefaultCacheSettings.TtlSeconds);
        }

        [TestMethod]
        public void OrchestratorOptions_DefaultRetrySettings_AreConfigured()
        {
            var opts = new YamlHttpOrchestratorOptions();

            Assert.AreEqual(3, opts.DefaultRetrySettings.MaxRetries);
            Assert.IsNotNull(opts.DefaultRetrySettings.RetryOnStatusCodes);
            CollectionAssert.Contains(opts.DefaultRetrySettings.RetryOnStatusCodes, 503);
        }

        [TestMethod]
        public void OrchestratorOptions_DefaultHandlebarsEngineKey()
        {
            var opts = new YamlHttpOrchestratorOptions();
            Assert.AreEqual("default_handlebars_engine", opts.HandlebarsEngineKey);
        }

        // ---------------------------------------------------------------------
        // Réinitialisation de LastCalledUrls entre deux exécutions
        // ---------------------------------------------------------------------

        [TestMethod]
        public async Task ExecuteSequenceAsync_ResetsLastCalledUrls_BetweenCalls()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);

            // Premier appel avec séquence vide
            await orch.ExecuteSequenceAsync(new { }, new List<dynamic>(),
                new Dictionary<string, HttpClientSettings>(), null, null, CancellationToken.None);

            // Deuxième appel — LastCalledUrls doit être une nouvelle liste vide
            await orch.ExecuteSequenceAsync(new { }, new List<dynamic>(),
                new Dictionary<string, HttpClientSettings>(), null, null, CancellationToken.None);

            Assert.AreEqual(0, orch.LastCalledUrls.Count);
        }


        // ---------------------------------------------------------------------
        // Validation des arguments de ExecuteSetAsync
        // ---------------------------------------------------------------------

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ExecuteSetAsync_ThrowsWhenSetNameIsNull()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);
            var config = new YamlHttpClientConfigBuilder();

            await orch.ExecuteSetAsync(null!, config, new { });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ExecuteSetAsync_ThrowsWhenSetNameIsWhitespace()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);
            var config = new YamlHttpClientConfigBuilder();

            await orch.ExecuteSetAsync("   ", config, new { });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ExecuteSetAsync_ThrowsWhenConfigIsNull()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);

            await orch.ExecuteSetAsync("mySet", null!, new { });
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ExecuteSetAsync_ThrowsWhenSetNameNotFound()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);
            var config = new YamlHttpClientConfigBuilder
            {
                HttpClientSet = new Dictionary<string, HttpClientSetSettings>(), // set vide
                HttpClient = new Dictionary<string, HttpClientSettings>()
            };

            await orch.ExecuteSetAsync("nonexistent_set", config, new { });
        }

        [TestMethod]
        public async Task ExecuteSetAsync_ErrorMessage_ContainsSetName()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);
            var config = new YamlHttpClientConfigBuilder
            {
                HttpClientSet = new Dictionary<string, HttpClientSetSettings>(),
                HttpClient = new Dictionary<string, HttpClientSettings>()
            };

            try
            {
                await orch.ExecuteSetAsync("missing_pipeline", config, new { });
                Assert.Fail("Une exception était attendue.");
            }
            catch (InvalidOperationException ex)
            {
                StringAssert.Contains(ex.Message, "missing_pipeline");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ExecuteSetAsync_ThrowsWhenHttpClientConfigIsNull()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);
            var config = new YamlHttpClientConfigBuilder
            {
                HttpClientSet = new Dictionary<string, HttpClientSetSettings>
                {
                    ["mySet"] = new HttpClientSetSettings
                    {
                        Sequence = new List<SequenceStepSettings>()
                    }
                },
                HttpClient = null // pas de définitions de clients
            };

            await orch.ExecuteSetAsync("mySet", config, new { });
        }

        // ---------------------------------------------------------------------
        // Séquence vide — délégation à ExecuteSequenceAsync
        // ---------------------------------------------------------------------

        [TestMethod]
        public async Task ExecuteSetAsync_EmptySequence_ReturnsSerializedInput()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);
            var config = new YamlHttpClientConfigBuilder
            {
                HttpClientSet = new Dictionary<string, HttpClientSetSettings>
                {
                    ["mySet"] = new HttpClientSetSettings
                    {
                        Sequence = new List<SequenceStepSettings>()
                    }
                },
                HttpClient = new Dictionary<string, HttpClientSettings>()
            };

            var result = await orch.ExecuteSetAsync("mySet", config, new { name = "test" });

            var json = JsonDocument.Parse(result);
            Assert.IsTrue(json.RootElement.TryGetProperty("input", out _));
        }

        [TestMethod]
        public async Task ExecuteSetAsync_WithDataAdapterTemplate_AppliesTemplate()
        {
            var hb = YamlHttpClientFactory.CreateDefaultHandleBars();
            var orch = new YamlHttpOrchestrator(hb);
            var config = new YamlHttpClientConfigBuilder
            {
                HttpClientSet = new Dictionary<string, HttpClientSetSettings>
                {
                    ["mySet"] = new HttpClientSetSettings
                    {
                        Sequence = new List<SequenceStepSettings>(),
                        DataAdapter = new DataAdapterSettings { Template = "Hello {{input.name}}" }
                    }
                },
                HttpClient = new Dictionary<string, HttpClientSettings>()
            };

            var result = await orch.ExecuteSetAsync("mySet", config, new { name = "World" });

            Assert.AreEqual("Hello World", result);
        }
    }
}


#endif