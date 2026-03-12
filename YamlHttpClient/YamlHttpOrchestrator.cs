#if NET6_0_OR_GREATER
using HandlebarsDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YamlHttpClient.Settings;
using YamlHttpClient.Utils;

namespace YamlHttpClient
{
    public class YamlHttpOrchestrator
    {
        private readonly IHandlebars _handlebarsEngine;
        private readonly YamlHttpOrchestratorOptions _options;

        public IReadOnlyList<string> LastCalledUrls { get; private set; } = new List<string>();

        public YamlHttpOrchestrator(IHandlebars handlebarsEngine, YamlHttpOrchestratorOptions? options = null)
        {
            _handlebarsEngine = handlebarsEngine ?? throw new ArgumentNullException(nameof(handlebarsEngine));
            _options = options ?? new YamlHttpOrchestratorOptions();
        }

        /// <summary>
        /// Executes a named pipeline defined in <c>http_client_set</c>, resolving all
        /// HTTP client definitions from <c>http_client</c>.
        /// </summary>
        /// <param name="setName">Key of the pipeline in <see cref="YamlHttpClientConfigBuilder.HttpClientSet"/>.</param>
        /// <param name="config">The full parsed YAML config (must contain both <c>http_client</c> and <c>http_client_set</c>).</param>
        /// <param name="inputData">Input data available as <c>input</c> in all Handlebars templates.</param>
        /// <param name="defaultHttpTimeout">Timeout applied to every HTTP call in the pipeline.</param>
        /// <param name="ct">Cancellation token.</param>
        public Task<string> ExecuteSetAsync(
            string setName,
            YamlHttpClientConfigBuilder config,
            dynamic inputData,
            TimeSpan? defaultHttpTimeout = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(setName))
                throw new ArgumentNullException(nameof(setName));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (config.HttpClientSet == null || !config.HttpClientSet.TryGetValue(setName, out var set))
                throw new InvalidOperationException($"Pipeline '{setName}' was not found in http_client_set.");
            if (config.HttpClient == null)
                throw new InvalidOperationException("The config does not contain any http_client definitions.");

            // Convert typed SequenceStepSettings to the dynamic shape ExecuteSequenceAsync expects
            var steps = set.Sequence.Select(s => (dynamic)new { HttpClient = s.HttpClient, As = s.As ?? s.HttpClient });

            return ExecuteSequenceAsync(
                inputData: inputData,
                sequenceAppels: steps,
                dictClientsConfig: config.HttpClient,
                dataAdapterTemplate: set.DataAdapter?.Template,
                defaultHttpTimeout: defaultHttpTimeout,
                ct: ct);
        }

        /// <summary>
        /// Executes an explicit sequence of HTTP calls and returns the result, optionally
        /// shaped by a Handlebars data adapter template.
        /// </summary>
        public async Task<string> ExecuteSequenceAsync(
                              dynamic inputData,
                              IEnumerable<dynamic> sequenceAppels,
                              Dictionary<string, HttpClientSettings> dictClientsConfig,
                              string? dataAdapterTemplate,
                              TimeSpan? defaultHttpTimeout,
                              CancellationToken ct)
        {
            if (sequenceAppels == null) throw new ArgumentNullException(nameof(sequenceAppels));
            if (dictClientsConfig == null) throw new ArgumentNullException(nameof(dictClientsConfig));

            Dictionary<string, object> aggregatedData = new Dictionary<string, object>()
                                                        {
                                                            { "input", inputData }
                                                        };

            // Réinitialisation à chaque exécution
            var calledUrls = new List<string>();
            LastCalledUrls = calledUrls;

            foreach (var step in sequenceAppels)
            {
                string clientName = step.HttpClient;
                string alias = step.As ?? clientName;

                if (!dictClientsConfig.TryGetValue(clientName, out var clientDef))
                    throw new InvalidOperationException($"HTTP client configuration for '{clientName}' was not found in the provided settings.");

                // Application des règles d'entreprise par défaut
                clientDef.Cache ??= _options.DefaultCacheSettings;
                clientDef.Retry ??= _options.DefaultRetrySettings;

                var client = new YamlHttpClientFactory(clientDef, defaultHttpTimeout ?? TimeSpan.FromSeconds(30), _handlebarsEngine);
               
                HttpResponseMessage response = await client.AutoCallAsync(aggregatedData, ct);

                // Capturer l'URL réellement appelée
                var calledUrl = client.LastResolvedUrl ?? clientName;
                calledUrls.Add(calledUrl);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync(ct);
                    throw new InvalidOperationException(
                        $"External API call '{clientName}' failed with status code {(int)response.StatusCode} ({response.StatusCode}). " +
                        $"URL: {calledUrl}. Response body: {errorBody}");
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                aggregatedData.Add(alias, new
                {
                    body = jsonResponse,
                    headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value)),
                    url = calledUrl
                });
            }

            // Exécution du Data Adapter Final
            if (!string.IsNullOrWhiteSpace(dataAdapterTemplate))
            {
                var templateCompile = _handlebarsEngine.CompileWithCache(dataAdapterTemplate);
                return templateCompile(aggregatedData);
            }

            return JsonSerializer.Serialize(aggregatedData);
        }
    }
}
#endif