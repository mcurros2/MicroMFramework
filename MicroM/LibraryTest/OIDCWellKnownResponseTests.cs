using MicroM.Configuration;
using MicroM.Core;
using MicroM.Web.Authentication.SSO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LibraryTest
{
    [TestClass]
    public class OIDCWellKnownResponseTests
    {
        private readonly JsonSerializerOptions _jsonOptionsUnsafe = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter() }
        };

        [TestMethod]
        public void SerializeDeserialize_IdTokenEncryptionAlg_RSA_OAEP()
        {
            // Arrange
            var app = new ApplicationOption() { ApplicationID = "test" };
            // Create an RSA self-signed certificate so CreateWellKnown picks RSA-related capabilities
            using X509Certificate2 cert = CryptClass.CreateSelfSignedCertificate();

            // Act
            var wellKnown = WellKnownProvider.CreateWellKnown(app, "https://example.test/oidc", cert);

            string json = JsonSerializer.Serialize(wellKnown, _jsonOptionsUnsafe);

            // Assert JSON contains the string specified by JsonStringEnumMemberName on RSA_OAEP
            Assert.Contains("RSA-OAEP", "Serialized JSON must contain the string 'RSA-OAEP' for RSA_OAEP enum member.");

            // Deserialize back
            var deserialized = JsonSerializer.Deserialize<OIDCWellKnownResponse>(json, _jsonOptionsUnsafe);
            Assert.IsNotNull(deserialized, "Deserialized object should not be null.");

            var algs = deserialized.id_token_encryption_alg_values_supported;
            Assert.IsNotNull(algs, "id_token_encryption_alg_values_supported should not be null after deserialization.");
            Assert.IsNotEmpty(algs, "id_token_encryption_alg_values_supported should contain at least one algorithm.");

            // Verify the enum member maps back to the RSA_OAEP enum value
            Assert.AreEqual(OIDCKeyEncryptionAlgorithm.RSA_OAEP, algs[0], "Deserialized enum value should be OIDCKeyEncryptionAlgorithm.RSA_OAEP.");
        }
    }
}