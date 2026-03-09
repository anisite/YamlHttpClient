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

        public YamlHttpOrchestrator(IHandlebars handlebarsEngine, YamlHttpOrchestratorOptions? options = null)
        {
            _handlebarsEngine = handlebarsEngine ?? throw new ArgumentNullException(nameof(handlebarsEngine));
            _options = options ?? new YamlHttpOrchestratorOptions();
        }

        public async Task<string> ExecuteSequenceAsync(
                                    dynamic inputData,
                                    IEnumerable<dynamic> sequenceAppels, // À typer idéalement avec ton objet SequenceStep
                                    Dictionary<string, HttpClientSettings> dictClientsConfig,
                                    string? dataAdapterTemplate,
                                    TimeSpan? defaultHttpTimeout,
                                    CancellationToken ct)
        {
            if (sequenceAppels == null) throw new ArgumentNullException(nameof(sequenceAppels));
            if (dictClientsConfig == null) throw new ArgumentNullException(nameof(dictClientsConfig));

            // 🚀 INJECTION DU CONTEXTE INITIAL
            Dictionary<string, object> aggregatedData = new Dictionary<string, object>()
            {
                { "input", inputData }
            };

            foreach (var step in sequenceAppels)
            {
                string clientName = step.HttpClient;
                string alias = step.As ?? clientName;

                if (!dictClientsConfig.TryGetValue(clientName, out var clientDef))
                {
                    throw new InvalidOperationException($"HTTP client configuration for '{clientName}' was not found in the provided settings.");
                }

                // Application des règles d'entreprise par défaut
                clientDef.Cache ??= _options.DefaultCacheSettings;
                clientDef.Retry ??= _options.DefaultRetrySettings;

                var client = new YamlHttpClientFactory(clientDef, defaultHttpTimeout ?? TimeSpan.FromSeconds(30), _handlebarsEngine);

                HttpResponseMessage response = await client.AutoCallAsync(aggregatedData, ct);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync(ct);
                    throw new InvalidOperationException($"External API call '{clientName}' failed with status code {(int)response.StatusCode} ({response.StatusCode}). Response body: {errorBody}");
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

                aggregatedData.Add(alias, new { body = jsonResponse, headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value)) });
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