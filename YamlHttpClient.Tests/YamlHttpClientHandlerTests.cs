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
            
            YamlHttpClient httpClient = new YamlHttpClient("myHttpCall", yamlFile);

            var testObject = new
            {
                val1 = "Je suis FRW",
                System = new { CodeNT = @"mes\cotda05" }
            };

            var response = await httpClient.AutoCall(testObject);

            //Do something with response
        }
    }
}