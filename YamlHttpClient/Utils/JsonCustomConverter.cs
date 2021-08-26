using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
            return objectType == typeof(object) || base.CanConvert(objectType);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject
                || reader.TokenType == JsonToken.Null)
                return base.ReadJson(reader, objectType, existingValue, serializer);

            if (reader.TokenType == JsonToken.StartArray)
            {
                return serializer.Deserialize<object[]?>(reader);
            }

           /* if (reader.TokenType == JsonToken.String)
            {
                var data = _stubble.Render((string)reader.Value, _inboundData);
            }*/

            return serializer.Deserialize(reader);
        }
    }
}