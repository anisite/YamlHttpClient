using HandlebarsDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace YamlHttpClient.Utils
{
    /// <summary />
    public class JsonHelper
    {
        /// <summary />
        public static Dictionary<string, object?> DeserializeAndFlatten(string json,
                                                                        bool forceStringOutput,
                                                                        string flatten_separator = ".",
                                                                        string flatten_index_surrounder = "[{0}]")
        {
            Dictionary<string, object?> dict = new Dictionary<string, object?>();
            JToken token = JToken.Parse(json);
            FillDictionaryFromJToken(dict, token, "", forceStringOutput, flatten_separator, flatten_index_surrounder);
            return dict;
        }

        private static void FillDictionaryFromJToken(Dictionary<string, object?> dict, JToken token, string prefix, bool forceStringOutput, string flatten_separator, string flatten_index_surrounder)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (JProperty prop in token.Children<JProperty>())
                    {
                        FillDictionaryFromJToken(dict, prop.Value, Join(false, prefix, prop.Name, flatten_separator, flatten_index_surrounder), forceStringOutput, flatten_separator, flatten_index_surrounder);
                    }
                    break;

                case JTokenType.Array:
                    int index = 0;
                    foreach (JToken value in token.Children())
                    {
                        FillDictionaryFromJToken(dict, value, Join(true, prefix, index.ToString(), flatten_separator, flatten_index_surrounder), forceStringOutput, flatten_separator, flatten_index_surrounder);
                        index++;
                    }
                    break;

                default:
                    if (forceStringOutput)
                    {
                        var value = ((JValue)token).Value;
                        var strVal = value?.ToString();

                        if (value is DateTime dt)
                        {
                            if (dt.Date == dt)
                                strVal = dt.ToString("yyyy-MM-dd");
                            else
                                strVal = dt.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        dict.Add(prefix, strVal);
                    }
                    else
                        dict.Add(prefix, ((JValue)token).Value);

                    break;
            }
        }

        private static string Join(bool isArray, string prefix, string name, string flatten_separator, string flatten_index_surrounder)
        {
            var val = (string.IsNullOrEmpty(prefix) ? name : prefix + (isArray ? string.Format(flatten_index_surrounder, name) : flatten_separator + name));
            return val;
        }
    }
}
