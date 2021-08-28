using HandlebarsDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace YamlHttpClient.Utils
{
    /// <summary />
    public class JsonHelper
    {
        /// <summary />
        public static Dictionary<string, object?> DeserializeAndFlatten(string json, string flatten_separator = ".", string flatten_index_surrounder = "[{0}]")
        {
            Dictionary<string, object?> dict = new Dictionary<string, object?>();
            JToken token = JToken.Parse(json);
            FillDictionaryFromJToken(dict, token, "", flatten_separator, flatten_index_surrounder);
            return dict;
        }

        private static void FillDictionaryFromJToken(Dictionary<string, object?> dict, JToken token, string prefix, string flatten_separator, string flatten_index_surrounder)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (JProperty prop in token.Children<JProperty>())
                    {
                        FillDictionaryFromJToken(dict, prop.Value, Join(false, prefix, prop.Name, flatten_separator, flatten_index_surrounder), flatten_separator, flatten_index_surrounder);
                    }
                    break;

                case JTokenType.Array:
                    int index = 0;
                    foreach (JToken value in token.Children())
                    {
                        FillDictionaryFromJToken(dict, value, Join(true, prefix, index.ToString(), flatten_separator, flatten_index_surrounder), flatten_separator, flatten_index_surrounder);
                        index++;
                    }
                    break;

                default:
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
