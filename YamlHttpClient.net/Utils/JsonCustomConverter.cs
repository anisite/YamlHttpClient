using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Stubble.Core;
using Stubble.Core.Builders;
using Stubble.Helpers;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Stubble.Core.Interfaces;

namespace YamlHttpClient.Utils
{
    /// <summary></summary>
    public class JsonCustomConverter : CustomCreationConverter<IDictionary<object, object>>
    {
        //private readonly Regex _isDateReg = new Regex(@"^\d{4}\-(0[1-9]|1[012])\-(0[1-9]|[12][0-9]|3[01])$");
        //private readonly bool _convertirTypes;
        private readonly dynamic _inboundData;
        private readonly IStubbleRenderer _stubble;

        public JsonCustomConverter(IStubbleRenderer stubble, dynamic inboundData)
        {
            _stubble = stubble;
            _inboundData = inboundData;
        }

        public override IDictionary<object, object> Create(Type objectType)
        {
            return new Dictionary<object, object>();
        }

        public override bool CanConvert(Type objectType)
        {
            // in addition to handling IDictionary<string, object>
            // we want to handle the deserialization of dict value
            // which is of type object
            return objectType == typeof(object) || base.CanConvert(objectType);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            //Regex.Replace("test[0]", @"\[.*?\]", "");

            if (reader.TokenType == JsonToken.StartObject
                || reader.TokenType == JsonToken.Null)
                return base.ReadJson(reader, objectType, existingValue, serializer);

            // if the next token is not an object
            // then fall back on standard deserializer (strings, numbers etc.)

            /*if (reader.TokenType == JsonToken.String && reader.Value.Equals("true"))
            {
                return serializer.Deserialize<bool>(reader);
            }*/

            if (reader.TokenType == JsonToken.StartArray)
            {
                return serializer.Deserialize<object[]?>(reader);
            }

            if (reader.TokenType == JsonToken.String)
            {
                return _stubble.Render((string)reader.Value, _inboundData);
            }

            /*if (_convertirTypes)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    if (reader.Value is null)
                        return null;

                    if ((string)reader.Value == "true")
                        return true;

                    if ((string)reader.Value == "false")
                        return false;

                    if (_isDateReg.Match(reader.Value?.ToString()).Success)
                        return DateTime.ParseExact((string)reader.Value!, "yyyy-MM-dd", null);

                    if (int.TryParse((string)reader.Value!, out int num))
                        return num;

                    if (decimal.TryParse((string)reader.Value!, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal dec))
                        return dec;
                }
            }*/

            return serializer.Deserialize(reader);
        }
    }
}