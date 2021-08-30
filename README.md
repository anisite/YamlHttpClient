# YamlHttpClient
Yaml config based .net HttpClient

```csharp
// Sample object
var anyInputObject = new
{
	table = new[] { "v1", "v2" },
	date = new DateTime(2000, 1, 1),
	obj = new[] { new { test = 1 }, new { test = 2 } },
	val1 = new Dictionary<string, object>() { { "testkey", "testval" } },
	place = "urlPartDemo",
	System = new { CodeNT = "internalCode" }
};

// Source config
var file = @"myYamlConfig.yml";

// Core builder, load settings from Yaml source
YamlHttpClientFactory httpClient = new YamlHttpClientFactory(new YamlHttpClientConfigBuilder()
								      .LoadFromFile(file, "myHttpCall"));

// Build Http message
var request = httpClient.BuildRequestMessage(anyInputObject);

// Inspect content if needed
var readContent = await request.Content.ReadAsStringAsync();

// Send it
var response = await httpClient.SendAsync(request);

// Do something with response
var returnData = await response.Content.ReadAsStringAsync();

// Check some stuff from config
await httpClient.CheckResponseAsync(response);

```
