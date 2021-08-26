using HandlebarsDotNet;
using Newtonsoft.Json;

namespace YamlHttpClient.Utils
{
    public static class HandleBarsExtensions
    {
        public static void AddJsonHelper(this IHandlebars hb)
        {
            hb.RegisterHelper("Json", (output, context, arguments) =>
            {
                var value = arguments.At<object>(0);
                output.Write(JsonConvert.SerializeObject(value));
            });
        }
    }
}
