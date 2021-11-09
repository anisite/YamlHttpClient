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
        public static IHandlebars AddJsonHelper(this IHandlebars hb, JsonSerializerSettings? jsonSerializerSettings = null)
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
                    {
                        json = JsonConvert.SerializeObject(null);
                    }
                    else
                    {
                        if (forceString && !flatten)
                        {
                            json = JsonConvert.SerializeObject(values[0]?.ToString(), jsonSerializerSettings);
                        }
                        else
                        {
                            json = JsonConvert.SerializeObject(values[0], jsonSerializerSettings);
                        }
                    }
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

            return hb;
        }

        public static IHandlebars AddBase64(this IHandlebars hb)
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

            return hb;
        }

        public static IHandlebars AddIfCond(this IHandlebars hb, bool skipNull)
        {
            hb.RegisterHelper("ifCond", (writer, options, context, args) =>
            {
                if (args.Length != 3)
                {
                    writer.Write("ifCond:Wrong number of arguments");
                    return;
                }
                if (args[0] == null || args[0].GetType().Name == "UndefinedBindingResult")
                {
                    if (!skipNull)
                    {
                        writer.Write("ifCond:args[0] undefined");
                    }
                    return;
                }
                if (args[1] == null || args[1].GetType().Name == "UndefinedBindingResult")
                {
                    writer.Write("ifCond:args[1] undefined");
                    return;
                }
                if (args[2] == null || args[2].GetType().Name == "UndefinedBindingResult")
                {
                    if (!skipNull)
                    {
                        writer.Write("ifCond:args[2] undefined");
                    }
                    return;
                   
                }
                if (args[0].GetType().Name == "String")
                {
                    var val1 = args[0].ToString();
                    var val2 = args[2].ToString();

                    switch (args[1].ToString())
                    {
                        case ">":
                            if (val1!.Length > val2!.Length)
                            {
                                options.Template(writer, context);
                            }
                            else
                            {
                                options.Inverse(writer, context);
                            }
                            break;
                        case "=":
                        case "==":
                            if (val1 == val2)
                            {
                                options.Template(writer, context);
                            }
                            else
                            {
                                options.Inverse(writer, context);
                            }
                            break;
                        case "<":
                            if (val1!.Length < val2!.Length)
                            {
                                options.Template(writer, context);
                            }
                            else
                            {
                                options.Inverse(writer, context);
                            }
                            break;
                        case "!=":
                        case "<>":
                            if (val1 != val2)
                            {
                                options.Template(writer, context);
                            }
                            else
                            {
                                options.Inverse(writer, context);
                            }
                            break;
                    }
                }
                else
                {
                    object val1 = args[0];
                    object val2 = args[2];

                    switch (args[1].ToString())
                    {
                        case ">":
                            if (float.Parse(val1.ToString()!) > float.Parse(val2.ToString()!))
                            {
                                options.Template(writer, context);
                            }
                            else
                            {
                                options.Inverse(writer, context);
                            }
                            break;
                        case "=":
                        case "==":
                            if (val1.Equals(val2))
                            {
                                options.Template(writer, context);
                            }
                            else
                            {
                                options.Inverse(writer, context);
                            }
                            break;
                        case "<":
                            if (float.Parse(val1.ToString()!) < float.Parse(val2.ToString()!))
                            {
                                options.Template(writer, context);
                            }
                            else
                            {
                                options.Inverse(writer, context);
                            }
                            break;
                        case "!=":
                        case "<>":
                            if (!val1.Equals(val2))
                            {
                                options.Template(writer, context);
                            }
                            else
                            {
                                options.Inverse(writer, context);
                            }
                            break;
                    }
                }
            });

            return hb;
        }
    }
}
