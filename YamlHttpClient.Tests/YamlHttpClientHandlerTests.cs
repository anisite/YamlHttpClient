using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Arnath.StandaloneHttpClientFactory;
using YamlHttpClient;

namespace YamlHttpClient.Tests
{
    [TestClass()]
    public class YamlHttpClientHandlerTests
    {
        [TestMethod()]
        public async Task YamlHttpClientHandlerTest()
        {
            var yamlFile = @"../../../test1.yml";
            
            YamlHttpClientFactory httpClient = new YamlHttpClientFactory("myHttpCall", yamlFile);

            var testObject = new
            {
                val1 = "Je suis FRW",
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
            
        }
    }
}