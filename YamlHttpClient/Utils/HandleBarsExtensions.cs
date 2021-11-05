using HandlebarsDotNet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
            // Json Element
            hb.RegisterHelper("Json", (output, context, arguments) =>
            {
                var values = new List<object?>();

                var flatten = false;
                var forceString = false;
                var flatten_separator = ".";
                var flatten_index_surrounder = "[{0}]";

                foreach (var item in arguments)
                {
                    if (item is { } && item.ToString()!.StartsWith(">", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (item.ToString()!.StartsWith(">flatten", StringComparison.InvariantCultureIgnoreCase))
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
                    else
                    {
                        values.Add(item);
                    }
                }

                string? json;

                if (values.Count == 1)
                    if (values[0] is UndefinedBindingResult)
                        json = JsonConvert.SerializeObject(null);
                    else
                        json = JsonConvert.SerializeObject(values[0]);
                else if (values.Count == 0)
                    json = JsonConvert.SerializeObject(values);
                else
                    json = JsonConvert.SerializeObject(string.Join("", values));


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
