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

            YamlHttpClientFactory factory = new YamlHttpClientFactory("backendclient",
                                                @"C:\Users\Dany\source\repos\YamlHttpClient.net\YamlHttpClient.netTests\test1.yml"); // can be static
            var response = await factory.AutoCall(new { val1 = "titi" });



        }
    }
}