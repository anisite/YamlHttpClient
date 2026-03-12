using HandlebarsDotNet;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlHttpClient.Settings;

namespace YamlHttpClient.Utils
{
    /// <summary />
    public static class YamlHttpClientConfigExtensions
    {
        /// <summary>
        /// Load config from string
        /// </summary>
        /// <param name="yamlHttpClientConfig"></param>
        /// <param name="yamlToLoad"></param>
        /// <param name="keyConfigName"></param>
        /// <returns></returns>
        public static HttpClientSettings LoadFromString(this YamlHttpClientConfigBuilder yamlHttpClientConfig, string yamlToLoad, string keyConfigName)
        {
            var builder = new DeserializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            yamlHttpClientConfig = builder
                .Deserialize<YamlHttpClientConfigBuilder>(yamlToLoad);

            return yamlHttpClientConfig.HttpClient[keyConfigName];
        }

        [ExcludeFromCodeCoverage]
        public static HttpClientSettings LoadFromBytes(this YamlHttpClientConfigBuilder yamlHttpClientConfig, byte[] yamlFile, string keyConfigName)
        {
            var builder = new DeserializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            yamlHttpClientConfig = builder
                .Deserialize<YamlHttpClientConfigBuilder>(Encoding.Default.GetString(yamlFile));

            return yamlHttpClientConfig.HttpClient[keyConfigName];
        }

        [ExcludeFromCodeCoverage]
        public static HttpClientSettings LoadFromFile(this YamlHttpClientConfigBuilder yamlHttpClientConfig, string filePath, string keyConfigName)
        {
            using (var configFile = new StreamReader(filePath))
            {
                var builder = new DeserializerBuilder()
                    .WithNamingConvention(NullNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                yamlHttpClientConfig = builder
                    .Deserialize<YamlHttpClientConfigBuilder>(configFile);
            }

            return yamlHttpClientConfig.HttpClient[keyConfigName];
        }

    }
}
