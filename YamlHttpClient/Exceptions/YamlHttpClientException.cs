using System;

namespace YamlHttpClient.Exceptions
{
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