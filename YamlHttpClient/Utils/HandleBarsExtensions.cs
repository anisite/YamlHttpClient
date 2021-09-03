using HandlebarsDotNet;
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;

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
                var forceString = false;
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
                        else if (item.ToString()!.StartsWith(">forceString", System.StringComparison.InvariantCultureIgnoreCase))
                        {
                            forceString = true;
                        }
                    }
                }
                var json = JsonConvert.SerializeObject(value);

                if (flatten)
                {
                    var oo = JsonHelper.DeserializeAndFlatten(json, forceString, flatten_separator, flatten_index_surrounder);
                    json = JsonConvert.SerializeObject(oo);
                }

                output.Write(json);
            });
        }

        public static void AddBase64(this IHandlebars hb)
        {
            hb.RegisterHelper("Base64", (output, context, arguments) =>
            {
                if (arguments[0] is Image img)
                {
                    string b64;
                    using (MemoryStream _mStream = new MemoryStream())
                    {
                        img.Save(_mStream, img.RawFormat);
                        _mStream.Position = 0;
                        byte[] _imageBytes = _mStream.ToArray();
                        b64 = Convert.ToBase64String(_imageBytes);
                    }

                    output.Write(b64);

                }
                else if (!arguments[0].GetType().IsPrimitive)
                {

                    var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(arguments[0]));
                    var b64 = System.Convert.ToBase64String(plainTextBytes);
                    output.Write(b64);

                }
                //var b64 = Convert.FromBase64String(arguments[0]?.ToString() ?? string.Empty);
            });
        }
    }
}
