using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Arnath.StandaloneHttpClientFactory;
using YamlHttpClient;
using YamlHttpClient.Utils;
using YamlHttpClient.Settings;

namespace YamlHttpClient.Tests
{
    [TestClass()]
    public class YamlHttpClientHandlerTests
    {
        [TestMethod()]
        public async Task YamlHttpClientHandlerTest()
        {
            var yamlFile = @"../../../test1.yml";

            var str = System.IO.File.ReadAllText(yamlFile);

            YamlHttpClientFactory httpClient = new YamlHttpClientFactory(new YamlHttpClientConfigBuilder().LoadFromString(str, "myHttpCall"));

            var testObject = new
            {
                table = new[] { "v1", "v2" },
                obj = new[] { new { test = 1 }, new { test = 2 } },
                val1 = new Dictionary<string, object>() { { "testkey", "testval" } },
                place = "yty",
                System = new { CodeNT = @"mes\cotda05" }
            };

            // Build message
            var request = httpClient.BuildRequestMessage(testObject);

            // Inspect content if needed
            var readContent = await request.Content.ReadAsStringAsync();

            // Send it
            var response = await httpClient.SendAsync(request);

            //Do something with response
            await httpClient.CheckResponseAsync(response);

            var data = await response.Content.ReadAsStringAsync();
        }
    }
}