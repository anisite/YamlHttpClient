using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using YamlHttpClient.Settings;

namespace YamlHttpClient
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddYamlHttpClientAccessor(this IServiceCollection services)
        {
            services.AddOptions();

            services.AddTransient<IYamlHttpClientAccessor, YamlHttpClientFactory>();

            return services;
        }
    }
}