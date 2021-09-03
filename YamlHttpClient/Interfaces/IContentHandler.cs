using System.Net.Http;
using YamlHttpClient.Settings;

namespace YamlHttpClient
{
    public interface IContentHandler
    {
        HttpContent? Content(dynamic data, ContentSettings contentSettings);
        string ParseContent(string input, object testObject);
    }
}