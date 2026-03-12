# YamlHttpClient
YAML config-based .NET HttpClient with retry, caching, chaos engineering, and multi-step orchestration.

Download from NuGet: https://www.nuget.org/packages/YamlHttpClient/

---

## Table of contents

- [Basic usage](#basic-usage)
- [YAML config reference](#yaml-config-reference)
- [Loading config](#loading-config)
- [Dependency injection](#dependency-injection)
- [AutoCallAsync](#autocallasync)
- [Retry](#retry)
- [Cache](#cache)
- [Chaos Monkey](#chaos-monkey)
- [Orchestrator](#orchestrator)
- [Handlebars helpers](#handlebars-helpers)
- [Features checklist](#features-checklist)

---

## Basic usage

```csharp
var anyInputObject = new
{
    table = new[] { "v1", "v2" },
    date  = new DateTime(2000, 1, 1),
    obj   = new[] { new { test = 1 }, new { test = 2 } },
    val1  = new Dictionary<string, object>() { { "testkey", "testval" } },
    place = "urlPartDemo",
    System = new { CodeNT = "internalCode" }
};

// Load settings from a YAML file, targeting a named key
YamlHttpClientFactory httpClient = new YamlHttpClientFactory(
    new YamlHttpClientConfigBuilder().LoadFromFile("myYamlConfig.yml", "myHttpCall"));

// Build the HTTP message — placeholders are resolved from anyInputObject
var request = httpClient.BuildRequestMessage(anyInputObject);

// Optionally inspect the built content
var readContent = await request.Content.ReadAsStringAsync();

// Send
var response = await httpClient.SendAsync(request);

// Read response
var returnData = await response.Content.ReadAsStringAsync();

// Assert response matches check_response rules from config (throws if not)
await httpClient.CheckResponseAsync(response);
```

Or use the shorthand that combines build + send in one call:

```csharp
var response = await httpClient.AutoCallAsync(anyInputObject);
await httpClient.CheckResponseAsync(response);
```

---

## YAML config reference

```yaml
http_client:
  myHttpCall:
    method: POST
    url: https://api.example.com/{{place}}/endpoint

    # NTLM auto-negotiation (app pool identity)
    use_default_credentials: true

    # HTTP Basic authentication
    # auth_basic: 'username:password'

    headers:
      CodeNT: '{{System.CodeNT}}'
      Accept: 'application/json'

    content:
      # JSON body with Handlebars templating
      json_content: |
        {
          "someVal": "{{val1}}",
          "flattenObj": {{{Json . ">flatten;_;_{0}" ">forcestring"}}}
          "obj": {{{Json .}}}
        }
      # Other content types (pick one):
      # string_content: 'raw text {{val}}'
      # form_content:
      #   field1: value1
      #   field2: '{{val}}'

    # Throws if the response body does not match expectations
    check_response:
      throw_exception_if_body_contains_any:
        - error
      throw_exception_if_body_not_contains_all:
        - success

    retry:
      max_retries: 3
      delay_milliseconds: 500
      retry_on_status_codes:
        - 500
        - 502
        - 503

    cache:
      enabled: true
      ttl_seconds: 120

    chaos:
      enabled: false
      injection_rate_percentage: 33
      delay_milliseconds: 200
      simulate_status_code: 503
      simulate_network_exception: false
```

---

## Loading config

Three loaders are available on `YamlHttpClientConfigBuilder`:

```csharp
// From a file path
var settings = new YamlHttpClientConfigBuilder().LoadFromFile("config.yml", "myHttpCall");

// From a YAML string (e.g. loaded from a database or environment variable)
var settings = new YamlHttpClientConfigBuilder().LoadFromString(yamlString, "myHttpCall");

// From a byte array (e.g. embedded resource)
var settings = new YamlHttpClientConfigBuilder().LoadFromBytes(yamlBytes, "myHttpCall");

var httpClient = new YamlHttpClientFactory(settings);
```

---

## Dependency injection

```csharp
// Startup / Program.cs
services.AddYamlHttpClientAccessor();

// In your service
public class MyService
{
    private readonly IYamlHttpClientAccessor _client;

    public MyService(IYamlHttpClientAccessor client)
    {
        _client = client;
        _client.HttpClientSettings = new YamlHttpClientConfigBuilder()
            .LoadFromFile("config.yml", "myHttpCall");
        _client.HandlebarsProvider = YamlHttpClientFactory.CreateDefaultHandleBars();
    }

    public async Task<string> CallAsync(object data)
    {
        var response = await _client.AutoCallAsync(data);
        await _client.CheckResponseAsync(response);
        return await response.Content.ReadAsStringAsync();
    }
}
```

---

## AutoCallAsync

`AutoCallAsync` is a convenience method that wraps `BuildRequestMessage` + `SendAsync(Func<HttpRequestMessage>)` in a single call. It integrates with the retry and cache pipelines automatically.

```csharp
// Without cancellation token
var response = await httpClient.AutoCallAsync(myData);

// With cancellation token
var response = await httpClient.AutoCallAsync(myData, cancellationToken);
```

> Use `AutoCallAsync` instead of `BuildRequestMessage` + `SendAsync` whenever you want retry and cache to work together correctly — the factory-based overload of `SendAsync` is required for those features.

---

## Retry

Retry is configured per HTTP client in YAML. The engine retries on specified HTTP status codes and on transient network exceptions (`HttpRequestException`, timeout).

```yaml
http_client:
  myHttpCall:
    method: GET
    url: https://api.example.com/data
    retry:
      max_retries: 3          # Number of retries after the initial attempt
      delay_milliseconds: 500 # Wait between attempts
      retry_on_status_codes:  # Only retry on these codes; omit to retry only on exceptions
        - 500
        - 502
        - 503
        - 504
```

```csharp
var settings = new YamlHttpClientConfigBuilder().LoadFromString(yaml, "myHttpCall");
var httpClient = new YamlHttpClientFactory(settings);

// AutoCallAsync handles retries transparently
var response = await httpClient.AutoCallAsync(myData);
```

---

## Cache

Responses are cached in memory keyed by `Method + URL + body hash`. Only successful responses (2xx) are cached. The cache is shared across all instances for the same URL.

```yaml
http_client:
  myHttpCall:
    method: GET
    url: https://api.example.com/reference-data
    cache:
      enabled: true
      ttl_seconds: 600  # 10 minutes; default is 600
```

```csharp
var settings = new YamlHttpClientConfigBuilder().LoadFromString(yaml, "myHttpCall");
var httpClient = new YamlHttpClientFactory(settings);

// First call hits the network; subsequent identical calls return from cache
var response = await httpClient.AutoCallAsync(myData);
```

> Cache and retry work together: the cache is checked before the retry loop, and a successful retry response is stored in cache.

---

## Chaos Monkey

Chaos Monkey injects failures into your HTTP calls at a configurable rate. This is useful for testing resilience locally or in a staging environment without needing an unstable external service.

```yaml
http_client:
  myHttpCall:
    method: POST
    url: https://api.example.com/endpoint
    chaos:
      enabled: true
      injection_rate_percentage: 30  # 30% of calls will be affected
      delay_milliseconds: 300        # Always add 300ms delay (regardless of injection rate)
      simulate_status_code: 503      # Return a fake 503 on affected calls
      # simulate_network_exception: true  # Throw HttpRequestException instead
```

The three chaos modes (combinable):

| Option | Effect |
|---|---|
| `delay_milliseconds` | Adds a fixed delay to every call |
| `simulate_status_code` | Returns a fake HTTP response with that status code |
| `simulate_network_exception: true` | Throws an `HttpRequestException` (simulates DNS failure, connection reset, etc.) |

`simulate_status_code` and `simulate_network_exception` are only triggered when a call falls within the `injection_rate_percentage`. `delay_milliseconds` is always applied when set.

```csharp
// Chaos is fully transparent to calling code — no changes needed
var response = await httpClient.AutoCallAsync(myData);
```

Combining chaos with retry lets you verify your retry policy actually recovers from failures:

```yaml
http_client:
  myHttpCall:
    method: GET
    url: https://api.example.com/data
    chaos:
      enabled: true
      injection_rate_percentage: 50
      simulate_status_code: 503
    retry:
      max_retries: 3
      delay_milliseconds: 100
      retry_on_status_codes:
        - 503
```

---

## Orchestrator

`YamlHttpOrchestrator` (requires .NET 6+) executes named pipelines defined in `http_client_set`. Each pipeline runs an ordered sequence of HTTP calls; every step's response is aggregated and made available to subsequent steps and to the final `data_adapter` template via Handlebars.

### YAML config

A single YAML file contains both `http_client_set` (the pipelines) and `http_client` (the individual client definitions). Each pipeline references its clients by name and defines its own `data_adapter` output template.

```yaml
http_client_set:
  users:
    sequence:
      - http_client: get_token
      - http_client: get_users
        as: get_users          # optional alias; defaults to http_client name
    data_adapter:
      template: |
        {
          "count": {{get_users.body.total}},
          "items": {{{Json get_users.body.records}}}
        }

  user_orders:
    sequence:
      - http_client: get_token
      - http_client: get_user
      - http_client: get_orders
    data_adapter:
      template: |
        {
          "customer": "{{get_user.body.name}}",
          "email":    "{{get_user.body.email}}",
          "orders":   {{{Json get_orders.body.records}}}
        }

http_client:
  get_token:
    method: POST
    url: https://auth.example.com/token
    content:
      json_content: |
        { "client_id": "{{input.clientId}}", "client_secret": "{{input.secret}}" }

  get_users:
    method: GET
    url: https://api.example.com/users
    headers:
      Authorization: 'Bearer {{get_token.body.access_token}}'
      Accept: 'application/json'
    retry:
      max_retries: 2
      delay_milliseconds: 500
      retry_on_status_codes: [500, 502, 503]
    chaos:
      enabled: false
      injection_rate_percentage: 50
      simulate_status_code: 500

  get_user:
    method: GET
    url: 'https://api.example.com/users/{{input.userId}}'
    headers:
      Authorization: 'Bearer {{get_token.body.access_token}}'
      Accept: 'application/json'

  get_orders:
    method: GET
    # References input data and a previous step's response
    url: 'https://api.example.com/orders?userId={{get_user.body.id}}'
    headers:
      Authorization: 'Bearer {{get_token.body.access_token}}'
      Accept: 'application/json'
```

> If `data_adapter.template` is omitted or blank, `ExecuteSetAsync` returns the full aggregated data object as raw JSON.

### Data model inside templates

After each step completes, its result is added to the aggregated data object under its alias (defaulting to the `http_client` name). The full shape available in any template is:

```
{
  input:      { ...your inputData... },
  get_token:  { body: {...}, headers: {...}, url: "https://..." },
  get_user:   { body: {...}, headers: {...}, url: "https://..." },
  get_orders: { body: {...}, headers: {...}, url: "https://..." }
}
```

Both `url` fields in `http_client` definitions and `data_adapter` templates have full access to this object via Handlebars.

### C# usage — `ExecuteSetAsync`

The preferred entry point. Loads the pipeline and all client definitions directly from the parsed config:

```csharp
using YamlHttpClient;
using YamlHttpClient.Settings;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// Parse the full YAML config (both http_client_set and http_client)
var yaml = File.ReadAllText("pipeline.yml");
var config = new DeserializerBuilder()
    .WithNamingConvention(NullNamingConvention.Instance)
    .IgnoreUnmatchedProperties()
    .Build()
    .Deserialize<YamlHttpClientConfigBuilder>(yaml);

var handlebars = YamlHttpClientFactory.CreateDefaultHandleBars();
var orchestrator = new YamlHttpOrchestrator(handlebars);

// Execute a named pipeline by key
var result = await orchestrator.ExecuteSetAsync(
    setName:            "regions",
    config:             config,
    inputData:          new { clientId = "my-app", secret = "s3cr3t" },
    defaultHttpTimeout: TimeSpan.FromSeconds(30),
    ct:                 CancellationToken.None
);

Console.WriteLine(result);

// Inspect which URLs were actually called
foreach (var url in orchestrator.LastCalledUrls)
    Console.WriteLine($"Called: {url}");
```

### C# usage — `ExecuteSequenceAsync` (advanced)

Use this overload when the sequence and data adapter template are defined in C# rather than in YAML:

```csharp
var steps = new List<dynamic>
{
    new { HttpClient = "get_token",  As = "get_token"  },
    new { HttpClient = "get_regions", As = "get_regions" }
};

var result = await orchestrator.ExecuteSequenceAsync(
    inputData:           new { clientId = "my-app", secret = "s3cr3t" },
    sequenceAppels:      steps,
    dictClientsConfig:   config.HttpClient,
    dataAdapterTemplate: "{{get_regions.body.result.records}}",
    defaultHttpTimeout:  TimeSpan.FromSeconds(30),
    ct:                  CancellationToken.None
);
```

### Default options

`YamlHttpOrchestratorOptions` provides fallback cache and retry settings applied to any step that does not define its own:

| Option | Default |
|---|---|
| `DefaultCacheSettings.Enabled` | `true` |
| `DefaultCacheSettings.TtlSeconds` | `1200` (20 min) |
| `DefaultRetrySettings.MaxRetries` | `3` |
| `DefaultRetrySettings.RetryOnStatusCodes` | `500, 501, 502, 503, 504` |

Pass `null` for `options` to use these defaults, or override selectively.

---

## Handlebars helpers

All templates (URL, headers, body, data adapter) are processed with [Handlebars.Net](https://github.com/Handlebars-Net/Handlebars.Net).

### `{{{Json VAR}}}`

Serializes a variable to JSON.

```handlebars
{{{Json obj}}}                                  → {"test":1}
{{{Json obj ">forcestring"}}}                   → "{\"test\":1}"
{{{Json val1 val2}}}                            → "concatenated strings"
```

### `{{{Json VAR ">flatten;SEP;IDX"}}}`

Flattens a nested object to a single-level dictionary.

```handlebars
{{{Json . ">flatten;.;[{0}]"}}}                 → {"obj[0].test":1,"obj[1].test":2}
{{{Json . ">flatten;_;_{0}"}}}                  → {"obj_0_test":1,"obj_1_test":2}
{{{Json . ">flatten;_;_{0}" ">forcestring"}}}   → {"obj_0_test":"1","obj_1_test":"2"}
```

### `{{#ifCond A OP B}}`

Conditional block helper. Supported operators: `=`, `==`, `!=`, `<>`, `<`, `>`, `contains`, `in`.

```handlebars
{{#ifCond status '=' 'active'}}Active{{else}}Inactive{{/ifCond}}
{{#ifCond roles 'contains' 'admin'}}Has admin{{/ifCond}}
```

### `{{{Base64 VAR}}}`

Encodes an object or image to Base64.

```handlebars
{{{Base64 myObject}}}
```

### Template caching

Templates are compiled once and cached automatically via `CompileWithCache`. Call `HandleBarsExtensions.ClearTemplateCache()` if you need to reload templates at runtime (e.g. hot reload of YAML config).

---

## Features checklist

- :white_check_mark: All HTTP methods (GET, POST, PUT, DELETE, PATCH...)
- :white_check_mark: Any request headers with Handlebars templating
- :white_check_mark: JSON, string, form data and binary (Base64) content types
- :white_check_mark: Basic HTTP authentication
- :white_check_mark: NTLM authentication (app pool auto-negotiation)
- :white_check_mark: Response validation with configurable rules (`check_response`)
- :white_check_mark: Automatic retry with configurable status codes and delay
- :white_check_mark: In-memory response cache with TTL
- :white_check_mark: Chaos Monkey (delay, fake status code, network exception simulation)
- :white_check_mark: Multi-step HTTP orchestration with data aggregation (`YamlHttpOrchestrator`, .NET 6+)
- :white_check_mark: Dependency injection support (`IYamlHttpClientAccessor`)
- :white_large_square: NTLM with explicit user/password
- :white_large_square: Client certificate authentication