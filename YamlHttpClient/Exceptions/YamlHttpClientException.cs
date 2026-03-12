using System;
using System.Diagnostics.CodeAnalysis;

namespace YamlHttpClient.Exceptions
{
    [ExcludeFromCodeCoverage]
    public class YamlHttpClientException : Exception
    {
        public YamlHttpClientException()
        {
        }

        public YamlHttpClientException(string? message) : base(message)
        {
        }

        public YamlHttpClientException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}