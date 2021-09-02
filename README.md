# YamlHttpClient
Yaml config based .net HttpClient

Download from NuGet
https://www.nuget.org/packages/YamlHttpClient/

## How to use

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

// Here the magic - Build Http message - Dynamically
// from config with your object as data source, see yaml config bellow
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

## Yaml config sample
```yaml
http_client:
  # Named config key
  myHttpCall:
      method: POST
      url: https://ptsv2.com/t/{{place}}/post
      # Ntlm auto negociation
      use_default_credentials: true
      # Any specific required headers
      headers:
          CodeNT: '{{System.CodeNT}}'
          Accept: 'application/json'
      # Example Json content to send, with token template value replacement by Handlebars.net
      json_content: |
        {
            "someVal": "{{val1}}", 
            "flattenObj": {{{Json . ">flatten;_;_{0}" ">forcestring"}}}
            "obj": {{{Json .}}}
         }
      # Quality assurance if supported by implementation to self check response raw body
      check_response:
        throw_exception_if_body_contains_any:
            - error
        throw_exception_if_body_not_contains_all:
            - dump
```

```Handlebars
    "someVal": "{{val1}}", 
```
