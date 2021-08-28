using HandlebarsDotNet;
using Newtonsoft.Json;

namespace YamlHttpClient.Utils
{
    /// <summary />
    public static class HandleBarsExtensions
    {
        /// <summary>
        /// Add {{Json myVariable}} to Handlebars
        /// </summary>
        public static void AddJsonHelper(this IHandlebars hb)
        {
            hb.RegisterHelper("Json", (output, context, arguments) =>
            {
                var value = new object();

                var isFirst = true;
                var flatten = false;
                var flatten_separator = ".";
                var flatten_index_surrounder = "[{0}]";

                foreach (var item in arguments)
                {
                    if (isFirst)
                    {
                        value = item;
                        isFirst = false;
                    }
                    else
                    {
                        if (item.ToString()!.StartsWith(">flatten", System.StringComparison.InvariantCultureIgnoreCase))
                        {
                            flatten = true;

                            var flattenOptions = item.ToString()!.Split(';');

                            if (flattenOptions.Length > 1)
                            {
                                flatten_separator = flattenOptions[1];
                                if (flattenOptions.Length > 2)
                                {
                                    flatten_index_surrounder = flattenOptions[2];
                                }
                            }
                        }
                    }
                }
                var json = JsonConvert.SerializeObject(value);

                if (flatten)
                {
                    var oo = JsonHelper.DeserializeAndFlatten(json, flatten_separator, flatten_index_surrounder);
                    json = JsonConvert.SerializeObject(oo);
                }

                output.Write(json);
            });
        }
    }
}
