using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Arnath.StandaloneHttpClientFactory;
using YamlHttpClient;
using YamlHttpClient.Utils;
using YamlHttpClient.Settings;
using System.Drawing;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace YamlHttpClient.Tests
{
    [TestClass()]
    public class YamlHttpClientFactoryTests
    {
        [TestMethod()]
        public async Task YamlHttpClientHandler_IServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddYamlHttpClientAccessor();

            var provider = services.BuildServiceProvider();
            var restService = provider.GetRequiredService<IYamlHttpClientAccessor>();

            Assert.IsNotNull(restService);
        }

        [TestMethod()]
        public async Task YamlHttpClientHandler_IServiceProvider2()
        {
            var mock = new Mock<YamlHttpClientFactory>();

            mock.CallBase = true;

            mock.Setup(e => e.SendAsync(It.IsAny<HttpRequestMessage>())).ReturnsAsync(new HttpResponseMessage { Content = new StringContent("test") });

            var services = new ServiceCollection();
            services.AddTransient<IYamlHttpClientAccessor>(e => { return mock.Object; });

            var provider = services.BuildServiceProvider();
            var restService = provider.GetRequiredService<IYamlHttpClientAccessor>();

            restService.HttpClientSettings = new HttpClientSettings { Method = "POST", Url = "", Content = new ContentSettings { JsonContent = "{}" } };
            restService.HandlebarsProvider = YamlHttpClientFactory.CreateDefaultHandleBars();

            var demo = restService.BuildRequestMessage(new { test = "empty" });

            var test = await restService.SendAsync(demo);

            Assert.AreEqual("test", await test.Content.ReadAsStringAsync());
        }

        /* [TestMethod()]
         public async Task YamlHttpClientHandlerTest()
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
         }*/

        /* [TestMethod()]
         public async Task YamlHttpClientHandler_Multipart_Test()
         {
             var yamlFile = @"../../../test_multipart.yml";

             var str = System.IO.File.ReadAllText(yamlFile);

             YamlHttpClientFactory httpClient = new YamlHttpClientFactory(new YamlHttpClientConfigBuilder().LoadFromString(str, "myHttpCall"));

             var base64Png = "iVBORw0KGgoAAAANSUhEUgAAABIAAAASCAMAAABhEH5lAAAB0VBMVEUAAAAAVaoAarYGdLcAarYEdLYNdrgCbLgCbrgSfLoBbb0Nfr4AaboMebwEc8AFdcABbsADb8ADccAEcsAFdsAGd8AEc8EJeMEJecEAab0Aa7kAar0OfcEOfr4KeboEc8IRf74UfsIBbL8liMIEcL4tjMMCbb8fhMAlh8QGdcMEcL0GdcEFb8EFccEri8UqicIohr8Id8UvjMMBcPACc/BCsfBCtPAVj+8Wke633PC63fAFfP8HgP8Jhf9YwP9aw/8Dd/9bx/9Xvf8Bc/9dyv8Lif9Vuv9Ut/8Njf8Pkv8Qlv9cv//Z7v8CdP8FeP8Fef8Jgv8Lgv4NiP4Rlv8Xjv8Xnf8Yjf8Ykv8Ynf8gmv8ilP9EsP9HqP9Ktf9Ltv9Ts/9Xs/9XuP9fwP9juP9juv9kwv9lzP9szf9uuv9vwv91y/92wf940/97wf9+x/+Iz/+KyP+My/+Szf+S0f+W3P+Zz/+bzv+c1f+k3v+m2P+n1P+o1v+o2P+o2f+t2f+u2/+u3v+x3f+33/++4P++4f+/4v/H5P/P6v/R7P/S6//S7f/T7P/U6v/a7v/c7//d7//f8P/o9v/q9f/t9//u+P/z+v/1+v/3/P/4/P/6/f/7/f+lX7LbAAAAO3RSTlMABomKi4uNkJCQvsDExsfHx8fHx8fHyMjIycnKysvLzM3Nzs7R0dLU1NXW1tfX19nb3N38/P39/v7+/ivQupEAAADtSURBVBjTY2Bg5FQ1MtJXV5AFAW5mRgYGRgPLkpw0Fxgwk2NkYLNKd0QGpqwMyhX2qICXQS/LDhXIMGh4O3knOQFBqhMESDAoxjZM73Jzy584xQ0M4kQZxEPLuzvcM2cWJ7qDQHSECAO/V3Bje3z91Ml9KT6debWVXvwMfJ6eNa2RTf0Js4p6C5N9sj35GIQ9PKpbPAomhM/ImFQXk1vqIcYg5Opa1ewa1TOtLaTMFQTkGQRtISAMSttqMgjYoIIAQwYua1QQpM3AZOKADPwtOBgYpYydYcDXL9BcCxRgLDySICCtpKajq8LOyAAAspFVEWA58YIAAAAbdEVYdFNvZnR3YXJlAEFQTkcgQXNzZW1ibGVyIDMuMF5FLBwAAAAASUVORK5CYII=";

             var testObject = new
             {
                 table = new[] { "v1", "v2" },
                 date = new DateTime(2000, 1, 1),
                 date2 = new DateTime(2000, 1, 1, 2, 2, 2),
                 obj = new[] { new { test = 1 }, new { test = 2 } },
                 val1 = new Dictionary<string, object>() { { "testkey", "testval" } },
                 place = "ytyy",
                 base64Png = base64Png,
                 Png = LoadBase64(base64Png)
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
         }*/

        public static Image LoadBase64(string base64)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            Image image;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                image = Image.FromStream(ms);
            }
            return image;
        }
    }
}