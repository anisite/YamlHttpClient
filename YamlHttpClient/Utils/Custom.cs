using HandlebarsDotNet;
using HandlebarsDotNet.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace YamlHttpClient.Utils
{
    public sealed class CustomJsonFormatter : IFormatter, IFormatterProvider
    {
        public void Format<T>(T value, in EncodedTextWriter writer)
        {
            writer.Write(JsonConvert.SerializeObject(value));
        }

        public bool TryCreateFormatter(Type type, out IFormatter? formatter)
        {
            if (type.BaseType != typeof(Object))
            {
                formatter = null;
                return false;
            }

            formatter = this;
            return true;
        }
    }
}
