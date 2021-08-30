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

## Example Yaml config
```yaml
http_client:
  myHttpCall:
      method: POST
      url: https://ptsv2.com/t/{{place}}/post
      use_default_credentials: true
      headers:
          CodeNT: '{{System.CodeNT}}'
          Accept: 'application/json'
      #string_content: string
      json_content: |
        {
            "someVal": "{{val1}}", 
            "flattenObj": {{{Json . ">flatten;_;_{0}" ">forcestring"}}}
            "obj": {{{Json .}}}
         }
      check_response:
        throw_exception_if_body_contains_any:
            - error
        throw_exception_if_body_not_contains_all:
            - dump
```
