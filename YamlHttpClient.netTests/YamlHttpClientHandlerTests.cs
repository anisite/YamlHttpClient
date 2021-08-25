using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Arnath.StandaloneHttpClientFactory;
using YamlHttpClient.Factory;

namespace YamlHttpClient.Tests
{
    [TestClass()]
    public class YamlHttpClientHandlerTests
    {
        [TestMethod()]
        public async Task YamlHttpClientHandlerTest()
        {
            var yamlFile = @"C:\Users\infol\Documents\GitHub\YamlHttpClient.net\YamlHttpClient.netTests\test1.yml";
            YamlHttpClientFactory factory = new YamlHttpClientFactory("myHttpCall", yamlFile);

            var response = await factory.AutoCall(
                new
                {
                    val1 = "titi",
                    System = new { CodeNT = @"mes\cotda05" }
                });

            //Do something with response
        }
    }
}