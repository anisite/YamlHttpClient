using System;
using System.Runtime.Serialization;

namespace YamlHttpClient.Exceptions
{
    [Serializable]
    public class ThrowExceptionIfBodyNotContainAll : Exception
    {
        public ThrowExceptionIfBodyNotContainAll()
        {
        }

        public ThrowExceptionIfBodyNotContainAll(string? item) : base($"{item} not present.")
        {
        }

        public ThrowExceptionIfBodyNotContainAll(string? item, Exception? innerException) : base($"{item} not present.", innerException)
        {
        }

        protected ThrowExceptionIfBodyNotContainAll(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}