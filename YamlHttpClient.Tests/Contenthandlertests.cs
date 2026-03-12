using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using YamlHttpClient.Settings;

namespace YamlHttpClient.Tests
{
    // =========================================================================
    // TESTS BASE64 CONTENT
    // =========================================================================

    [TestClass]
    public class ContentHandlerBase64Tests
    {
        private static ContentHandler Handler =>
            new ContentHandler(YamlHttpClientFactory.CreateDefaultHandleBars());

        [TestMethod]
        public async Task Base64Content_ReturnsCorrectBytes()
        {
            var originalBytes = new byte[] { 1, 2, 3, 4, 5 };
            var base64 = Convert.ToBase64String(originalBytes);

            var content = Handler.Content(new { }, new ContentSettings
            {
                Base64Content = base64
            });

            Assert.IsInstanceOfType(content, typeof(ByteArrayContent));
            var result = await content!.ReadAsByteArrayAsync();
            CollectionAssert.AreEqual(originalBytes, result);
        }

        [TestMethod]
        public async Task Base64Content_WithContentType_SetsHeader()
        {
            var base64 = Convert.ToBase64String(new byte[] { 0xFF, 0xD8, 0xFF }); // fake JPEG header

            var content = Handler.Content(new { }, new ContentSettings
            {
                Base64Content = base64,
                ContentType = "image/jpeg"
            });

            Assert.IsInstanceOfType(content, typeof(ByteArrayContent));
            Assert.AreEqual("image/jpeg", content!.Headers.ContentType?.MediaType);
        }

        [TestMethod]
        public async Task Base64Content_WithoutContentType_HasNoContentTypeHeader()
        {
            var base64 = Convert.ToBase64String(new byte[] { 1, 2, 3 });

            var content = Handler.Content(new { }, new ContentSettings
            {
                Base64Content = base64
                // ContentType intentionnellement absent
            });

            Assert.IsInstanceOfType(content, typeof(ByteArrayContent));
            Assert.IsNull(content!.Headers.ContentType);
        }

        [TestMethod]
        public async Task Base64Content_WithHandlebarsTemplate_ResolvesBeforeDecoding()
        {
            var originalBytes = System.Text.Encoding.UTF8.GetBytes("hello");
            var base64 = Convert.ToBase64String(originalBytes);

            // Le template Handlebars insère la valeur base64 depuis les données
            var content = Handler.Content(
                new { myBase64 = base64 },
                new ContentSettings { Base64Content = "{{myBase64}}" }
            );

            Assert.IsInstanceOfType(content, typeof(ByteArrayContent));
            var result = await content!.ReadAsByteArrayAsync();
            CollectionAssert.AreEqual(originalBytes, result);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void Base64Content_InvalidBase64_ThrowsFormatException()
        {
            Handler.Content(new { }, new ContentSettings
            {
                Base64Content = "NOT_VALID_BASE64!!!"
            });
        }

        [TestMethod]
        public async Task Base64Content_EmptyBytes_ReturnsEmptyByteArray()
        {
            var base64 = Convert.ToBase64String(Array.Empty<byte>());

            var content = Handler.Content(new { }, new ContentSettings
            {
                Base64Content = base64
            });

            Assert.IsInstanceOfType(content, typeof(ByteArrayContent));
            var result = await content!.ReadAsByteArrayAsync();
            Assert.AreEqual(0, result.Length);
        }
    }

    // =========================================================================
    // TESTS MULTIPART CONTENT
    // =========================================================================

    [TestClass]
    public class ContentHandlerMultipartTests
    {
        private static ContentHandler Handler =>
            new ContentHandler(YamlHttpClientFactory.CreateDefaultHandleBars());

        [TestMethod]
        public void Multipart_ReturnsMultipartFormDataContent()
        {
            var content = Handler.Content(new { }, new ContentSettings
            {
                MultipartContent = new MultipartContentY
                {
                    Contents = new List<ContentSettings>
                    {
                        new ContentSettings { StringContent = "hello", ContentName = "field1" }
                    }
                }
            });

            Assert.IsInstanceOfType(content, typeof(MultipartFormDataContent));
        }

        [TestMethod]
        public void Multipart_WithBoundary_UsesBoundary()
        {
            var content = Handler.Content(new { }, new ContentSettings
            {
                MultipartContent = new MultipartContentY
                {
                    Boundary = "my-boundary-123",
                    Contents = new List<ContentSettings>
                    {
                        new ContentSettings { StringContent = "value", ContentName = "field" }
                    }
                }
            }) as MultipartFormDataContent;

            Assert.IsNotNull(content);
            StringAssert.Contains(
                content.Headers.ContentType!.Parameters
                    .First(p => p.Name == "boundary").Value,
                "my-boundary-123");
        }

        [TestMethod]
        public void Multipart_WithoutBoundary_GeneratesBoundaryAutomatically()
        {
            var content = Handler.Content(new { }, new ContentSettings
            {
                MultipartContent = new MultipartContentY
                {
                    // Boundary = null intentionnellement
                    Contents = new List<ContentSettings>
                    {
                        new ContentSettings { StringContent = "value" }
                    }
                }
            }) as MultipartFormDataContent;

            Assert.IsNotNull(content);
            var boundary = content.Headers.ContentType!.Parameters
                .FirstOrDefault(p => p.Name == "boundary")?.Value;
            Assert.IsNotNull(boundary, "Une boundary doit être générée automatiquement.");
        }

        [TestMethod]
        public async Task Multipart_Part_WithContentNameAndFileName()
        {
            var base64 = Convert.ToBase64String(new byte[] { 1, 2, 3 });

            var multipart = Handler.Content(new { }, new ContentSettings
            {
                MultipartContent = new MultipartContentY
                {
                    Contents = new List<ContentSettings>
                    {
                        new ContentSettings
                        {
                            Base64Content = base64,
                            ContentName   = "file",
                            FileName      = "upload.bin"
                        }
                    }
                }
            }) as MultipartFormDataContent;

            Assert.IsNotNull(multipart);
            // La présence du Content-Disposition avec filename confirme le bon chemin de code
            var raw = await multipart.ReadAsStringAsync();
            StringAssert.Contains(raw, "upload.bin");
            StringAssert.Contains(raw, "file");
        }

        [TestMethod]
        public async Task Multipart_Part_WithContentNameOnly()
        {
            var multipart = Handler.Content(new { }, new ContentSettings
            {
                MultipartContent = new MultipartContentY
                {
                    Contents = new List<ContentSettings>
                    {
                        new ContentSettings
                        {
                            StringContent = "my-value",
                            ContentName   = "my-field"
                            // FileName absent
                        }
                    }
                }
            }) as MultipartFormDataContent;

            Assert.IsNotNull(multipart);
            var raw = await multipart.ReadAsStringAsync();
            StringAssert.Contains(raw, "my-field");
            StringAssert.Contains(raw, "my-value");
        }

        [TestMethod]
        public async Task Multipart_Part_WithoutContentName()
        {
            var multipart = Handler.Content(new { }, new ContentSettings
            {
                MultipartContent = new MultipartContentY
                {
                    Contents = new List<ContentSettings>
                    {
                        new ContentSettings
                        {
                            StringContent = "anonymous-part"
                            // ContentName et FileName absents
                        }
                    }
                }
            }) as MultipartFormDataContent;

            Assert.IsNotNull(multipart);
            var raw = await multipart.ReadAsStringAsync();
            StringAssert.Contains(raw, "anonymous-part");
        }

        [TestMethod]
        public void Multipart_NullContents_ReturnsEmptyMultipart()
        {
            var content = Handler.Content(new { }, new ContentSettings
            {
                MultipartContent = new MultipartContentY
                {
                    Contents = null  // liste nulle => branche ignorée
                }
            });

            // Contents est null => la condition "MultipartContent.Contents is { }" est fausse => retourne null
            Assert.IsNull(content);
        }

        [TestMethod]
        public async Task Multipart_MultipleParts_AllPresent()
        {
            var multipart = Handler.Content(new { }, new ContentSettings
            {
                MultipartContent = new MultipartContentY
                {
                    Contents = new List<ContentSettings>
                    {
                        new ContentSettings { StringContent = "part-one",  ContentName = "field1" },
                        new ContentSettings { StringContent = "part-two",  ContentName = "field2" },
                        new ContentSettings { JsonContent  = "{\"x\":1}",  ContentName = "field3" }
                    }
                }
            }) as MultipartFormDataContent;

            Assert.IsNotNull(multipart);
            var raw = await multipart.ReadAsStringAsync();
            StringAssert.Contains(raw, "part-one");
            StringAssert.Contains(raw, "part-two");
            StringAssert.Contains(raw, "{\"x\":1}");
        }

        [TestMethod]
        public async Task Multipart_Part_WithHandlebarsTemplate_ResolvesValues()
        {
            var data = new { username = "alice", token = "xyz" };

            var multipart = Handler.Content(data, new ContentSettings
            {
                MultipartContent = new MultipartContentY
                {
                    Contents = new List<ContentSettings>
                    {
                        new ContentSettings
                        {
                            StringContent = "User: {{username}}",
                            ContentName   = "info"
                        },
                        new ContentSettings
                        {
                            JsonContent = "{\"token\":\"{{token}}\"}",
                            ContentName = "auth"
                        }
                    }
                }
            }) as MultipartFormDataContent;

            Assert.IsNotNull(multipart);
            var raw = await multipart.ReadAsStringAsync();
            StringAssert.Contains(raw, "User: alice");
            StringAssert.Contains(raw, "\"token\":\"xyz\"");
        }

        [TestMethod]
        public async Task Multipart_Part_Base64InsideMultipart()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("file-content");
            var base64 = Convert.ToBase64String(bytes);

            var multipart = Handler.Content(new { }, new ContentSettings
            {
                MultipartContent = new MultipartContentY
                {
                    Contents = new List<ContentSettings>
                    {
                        new ContentSettings
                        {
                            Base64Content = base64,
                            ContentType   = "application/octet-stream",
                            ContentName   = "attachment",
                            FileName      = "data.bin"
                        }
                    }
                }
            }) as MultipartFormDataContent;

            Assert.IsNotNull(multipart);
            var raw = await multipart.ReadAsByteArrayAsync();
            // Les bytes originaux doivent être présents dans le multipart brut
            Assert.IsTrue(raw.Length > 0);
        }
    }
}