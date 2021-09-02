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
using Xunit;

namespace YamlHttpClient.Tests
{
    [TestClass()]
    public class JsonParseTests
    {
        [Theory]
        [InlineData("ll/l", "")]
        public async Task YamlHttpClientHandlerTest(string value, string val2)
        {
            var yamlFile = @"../../../test1.yml";

            var str = System.IO.File.ReadAllText(yamlFile);

            YamlHttpClientFactory httpClient = new YamlHttpClientFactory(new YamlHttpClientConfigBuilder().LoadFromString(str, "myHttpCall"));

            var testObject = new
            {
                table = new[] { "v1", "v2" },
                date = new DateTime(2000, 1, 1),
                date2 = new DateTime(2000, 1, 1, 2, 2, 2),
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