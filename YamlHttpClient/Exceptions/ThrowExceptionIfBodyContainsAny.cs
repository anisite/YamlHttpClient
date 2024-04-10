using System;
using System.Runtime.Serialization;

namespace YamlHttpClient.Exceptions
{
    [Serializable]
    public class ThrowExceptionIfBodyContainsAny : Exception
    {
        public ThrowExceptionIfBodyContainsAny()
        {
        }

        public ThrowExceptionIfBodyContainsAny(string? item) : base($"{item} is present.")
        {
        }

        public ThrowExceptionIfBodyContainsAny(string? item, Exception? innerException) : base($"{item} is present.", innerException)
        {
        }

        protected ThrowExceptionIfBodyContainsAny(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}