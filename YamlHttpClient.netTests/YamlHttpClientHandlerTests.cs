using Microsoft.VisualStudio.TestTools.UnitTesting;
using YamlHttpClient.net;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Arnath.StandaloneHttpClientFactory;

namespace YamlHttpClient.net.Tests
{
    [TestClass()]
    public class YamlHttpClientHandlerTests
    {
        [TestMethod()]
        public async Task YamlHttpClientHandlerTest()
        {
            var client = new YamlHttpClient("backendclient", @"C:\Users\Dany\source\repos\YamlHttpClient.net\YamlHttpClient.netTests\test1.yml");

            var tt = await client.SendAsync();
        }
    }
}