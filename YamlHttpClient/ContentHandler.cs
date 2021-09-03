using HandlebarsDotNet;
using HandlebarsDotNet.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlHttpClient.Exceptions;
using YamlHttpClient.Factory;
using YamlHttpClient.Interfaces;
using YamlHttpClient.Settings;
using YamlHttpClient.Utils;

namespace YamlHttpClient
{
    /// <summary>
    /// Yaml config based HttpClient
    /// </summary>
    public class ContentHandler : IContentHandler
    {
        private readonly IHandlebars _handlebars;

        /// <summary>
        /// 
        /// </summary>
        public ContentHandler(IHandlebars handlebars)
        {
            _handlebars = handlebars;
        }

        /// <summary>
        /// Handle content conversion, template replacementm etc.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="contentSettings"></param>
        /// <returns></returns>
        public HttpContent? Content(dynamic data, ContentSettings contentSettings)
        {
            // String Content
            if (contentSettings.StringContent is { })
            {
                var result = ParseContent(contentSettings.StringContent, data);

                return new StringContent(result);
            }
            // Json Content
            else if (contentSettings.JsonContent is { })
            {
                var result = ParseContent(contentSettings.JsonContent, data);

                return new StringContent(result, Encoding.GetEncoding(contentSettings.Encoding ?? "UTF-8"), "application/json");
            }
            // Form Content
            else if (contentSettings.FormContent is { })
            {
                return new FormUrlEncodedContent(contentSettings.FormContent);
            }
            // Stream Content
            else if (contentSettings.Base64Content is { })
            {
                byte[] b = Convert.FromBase64String(ParseContent(contentSettings.Base64Content, data));
                var byteArrayContent = new ByteArrayContent(b);
                byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue(contentSettings.ContentType);
                return byteArrayContent;
            }
            // multipart Content
            else if (contentSettings.MultipartContent is { } && contentSettings.MultipartContent.Contents is { })
            {
                MultipartFormDataContent multipart;

                if (contentSettings.MultipartContent.Boundary is { })
                    multipart = new MultipartFormDataContent(contentSettings.MultipartContent.Boundary);
                else
                    multipart = new MultipartFormDataContent();

                foreach (var partContent in contentSettings.MultipartContent.Contents)
                {
                    HttpContent content = Content(data, partContent);

                    if (partContent.ContentName is { } && partContent.FileName is { })
                        multipart.Add(content, partContent.ContentName, partContent.FileName);
                    else if (partContent.ContentName is { })
                        multipart.Add(content, partContent.ContentName);
                    else
                        multipart.Add(content);

                }

                return multipart;
            }

            return null;
        }

        /// <summary>
        /// Internal parser, can be used to tests your templates
        /// </summary>
        /// <param name="contentTemplate"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string ParseContent(string? contentTemplate, dynamic data)
        {
            var template = _handlebars.Compile(contentTemplate ?? string.Empty);
            var result = template(data);
            return result;
        }
    }
}
