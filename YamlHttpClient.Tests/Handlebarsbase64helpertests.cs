using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using YamlHttpClient.Utils;

namespace YamlHttpClient.Tests
{
    // =========================================================================
    // TESTS HELPER HANDLEBARS {{Base64 ...}}
    // =========================================================================

    [TestClass]
    public class HandleBarsBase64HelperTests
    {
        /// <summary>
        /// Retourne un IHandlebars avec uniquement AddBase64 enregistré,
        /// sans AddJsonHelper pour éviter les conflits de helper "Json".
        /// </summary>
        private static ContentHandler MakeHandler() =>
            new ContentHandler(
                YamlHttpClientFactory.CreateEmptyHandleBars().AddBase64());

        // ------------------------------------------------------------------
        // Branche : objet non-primitif
        // ------------------------------------------------------------------

        [TestMethod]
        public void Base64Helper_Object_EncodesJsonRepresentation()
        {
            var data = new { myObj = new { key = "value", num = 42 } };

            var result = MakeHandler().ParseContent("{{{Base64 myObj}}}", data);

            // Décoder et vérifier que c'est le JSON de l'objet
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(result));
            var expected = JsonConvert.SerializeObject(data.myObj);
            Assert.AreEqual(expected, decoded);
        }

        [TestMethod]
        public void Base64Helper_Array_EncodesJsonRepresentation()
        {
            var data = new { items = new[] { "a", "b", "c" } };

            var result = MakeHandler().ParseContent("{{{Base64 items}}}", data);

            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(result));
            Assert.AreEqual("[\"a\",\"b\",\"c\"]", decoded);
        }

        [TestMethod]
        public void Base64Helper_Dictionary_EncodesJsonRepresentation()
        {
            var data = new
            {
                map = new Dictionary<string, object> { { "x", 1 }, { "y", "hello" } }
            };

            var result = MakeHandler().ParseContent("{{{Base64 map}}}", data);

            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(result));
            var expected = JsonConvert.SerializeObject(data.map);
            Assert.AreEqual(expected, decoded);
        }

        [TestMethod]
        public void Base64Helper_NestedObject_EncodesFullJson()
        {
            var data = new
            {
                payload = new
                {
                    user = new { id = 1, name = "Alice" },
                    roles = new[] { "admin", "user" }
                }
            };

            var result = MakeHandler().ParseContent("{{{Base64 payload}}}", data);

            Assert.IsFalse(string.IsNullOrEmpty(result));
            // Doit être un Base64 valide
            var bytes = Convert.FromBase64String(result);
            var decoded = System.Text.Encoding.UTF8.GetString(bytes);
            StringAssert.Contains(decoded, "Alice");
            StringAssert.Contains(decoded, "admin");
        }

        [TestMethod]
        public void Base64Helper_OutputIsValidBase64()
        {
            var data = new { obj = new { a = 1 } };
            var result = MakeHandler().ParseContent("{{{Base64 obj}}}", data);

            // Ne doit pas lever d'exception
            var bytes = Convert.FromBase64String(result);
            Assert.IsTrue(bytes.Length > 0);
        }

        [TestMethod]
        public void Base64Helper_ResultIsConsistent_SameInputSameOutput()
        {
            var data = new { obj = new { stable = "yes" } };
            var handler = MakeHandler();

            var r1 = handler.ParseContent("{{{Base64 obj}}}", data);
            var r2 = handler.ParseContent("{{{Base64 obj}}}", data);

            Assert.AreEqual(r1, r2, "Le même objet doit toujours produire le même Base64.");
        }

        // ------------------------------------------------------------------
        // Branche : string (est un non-primitif via GetType().IsPrimitive = false)
        // ------------------------------------------------------------------

        [TestMethod]
        public void Base64Helper_String_EncodesJsonSerializedString()
        {
            // string n'est pas un type primitif au sens de IsPrimitive,
            // donc passe dans la branche "non-primitif" et est sérialisé via JsonConvert
            var data = new { text = "hello world" };

            var result = MakeHandler().ParseContent("{{{Base64 text}}}", data);

            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(result));
            // JsonConvert.SerializeObject("hello world") => "\"hello world\""
            Assert.AreEqual("\"hello world\"", decoded);
        }

        // ------------------------------------------------------------------
        // Branche : Image (uniquement sur Windows où GDI+ est disponible)
        // ------------------------------------------------------------------

        [TestMethod]
        public void Base64Helper_Image_EncodesImageToBase64()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // System.Drawing.Common ne supporte pas GDI+ hors Windows
                Assert.Inconclusive("Ce test requiert Windows (System.Drawing.Common / GDI+).");
                return;
            }

            // PNG 1x1 pixel encodé en Base64 (format minimal valide)
            const string tiny1x1Png =
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

            var imageBytes = Convert.FromBase64String(tiny1x1Png);
            System.Drawing.Image img;
            using (var ms = new System.IO.MemoryStream(imageBytes))
            {
                img = System.Drawing.Image.FromStream(ms);
            }

            var data = new { img };
            var result = MakeHandler().ParseContent("{{{Base64 img}}}", data);

            Assert.IsFalse(string.IsNullOrEmpty(result), "Le helper doit produire un Base64 non vide.");
            // Doit être un Base64 valide
            var decoded = Convert.FromBase64String(result);
            Assert.IsTrue(decoded.Length > 0);
        }
    }
}