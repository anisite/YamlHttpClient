using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace YamlHttpClient.Exceptions
{
    [Serializable]
    [ExcludeFromCodeCoverage]
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