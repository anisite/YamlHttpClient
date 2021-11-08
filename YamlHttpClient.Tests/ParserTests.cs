using System;
using System.Collections.Generic;
using Xunit;

namespace YamlHttpClient.Tests
{

    public class ParserTests
    {
        [Theory]
        [InlineData("{{{Json obj.0.test \", \" obj.1.test}}}", @"""1, 2""")]
        [InlineData("{{{Json obj.88.test}}}", @"null")]
        [InlineData("{{{Json .}}}", @"{""obj"":[{""test"":1},{""test"":2}]}")]
        [InlineData(@"{{{Json . "">flatten;_;_{0}"" "">forcestring""}}}}", @"{""obj_0_test"":""1"",""obj_1_test"":""2""}")]
        [InlineData(@"{{{Json . "">flatten;_;_{0}""}}}}", @"{""obj_0_test"":1,""obj_1_test"":2}")]
        [InlineData(@"{{{Json . "">flatten;.;[{0}]""}}}}", @"{""obj[0].test"":1,""obj[1].test"":2}")]
        public void Simple_HandleBars_Formatters(string input, string expected)
        {
            var testObject = new
            {
                obj = new[] { new { test = 1 }, new { test = 2 } }
            };

            var result = new ContentHandler(YamlHttpClientFactory
                .CreateHandleBars())
                .ParseContent(input, testObject);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("{{{Json obj}}}", @"""2020-02-02""")]
        public void Date_HandleBars_Formatters(string input, string expected)
        {
            var testObject = new
            {
                obj = new DateTime(2020, 02, 02)
            };

            var result = new ContentHandler(YamlHttpClientFactory
                .CreateHandleBars(new Newtonsoft.Json.JsonSerializerSettings() { DateFormatString = "yyyy-MM-dd",  }))
                .ParseContent(input, testObject);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("{{{Json obj.0.test}}}", @"null")]
        [InlineData("{{{Json Indentite.GD_A_N_CIVQ_CORR}}}", @"null")]
        [InlineData("{{{Json Indentite2.GD_A_N_CIVQ_CORR}}}", @"null")]
        public void Dict_HandleBars_Formatters(string input, string expected)
        {

            var dict = new Dictionary<string, object> {
                {"Indentite", new Dictionary<string, object> {{ "GD_A_N_CIVQ_CORR", null } }},
                    { "Indentite2", null }
            };


            var result = new ContentHandler(YamlHttpClientFactory
                .CreateHandleBars())
                .ParseContent(input, dict);

            Assert.Equal(expected, result);
        }

        [Fact]
        //[InlineData(new Dictionary<string, string>() { { "", "" } }, @"{""obj"":[{""test"":1},{""test"":2}]}")]
        public void Simple_Form_Content()
        {
            var dict = new Dictionary<string, string>() {
                { "key1", "val1" },
                { "key2", "val2" }
            };

            var testObject = new
            {
                obj = new[] { new { test = 1 }, new { test = 2 } }
            };

            var result = new ContentHandler(YamlHttpClientFactory
                .CreateHandleBars())
                .Content(testObject, new Settings.ContentSettings() { FormContent = dict });

            Assert.Equal("key1=val1&key2=val2", result.ReadAsStringAsync().Result);
        }
    }
}