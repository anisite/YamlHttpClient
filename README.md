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
// from config with your object as data source, see yaml config below
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
      content:
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

Where VAR is from passed data.
```Handlebars
{{{Json VAR}}} # Simple object to json serialization
{{{Json VAR ">flatten;.;[{0}]"}}} # Flatten object to one level dictionary. Child naming childName[0].prop
{{{Json VAR ">flatten;_;_{0}" ">forcestring"}}} # Flatten object to one level dictionary. Child naming childName_0_prop. Force String values.
```

## Features support checklist
- :white_check_mark: Support all http methods, POST, GET, DELETE... 
- :white_check_mark: Send any header
- :white_check_mark: Send JSON, string, form data, binary files
- :white_check_mark: Basic Http Authentication 
- :white_check_mark: NTLM (with use default credentials, app pool auto authentication)
- :white_large_square: NTLM with user/password
- :white_large_square: Client certificate Authentication
