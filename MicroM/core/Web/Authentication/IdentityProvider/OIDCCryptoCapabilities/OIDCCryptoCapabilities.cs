using Microsoft.IdentityModel.Tokens;

namespace MicroM.Web.Authentication.SSO;

/// <summary>
/// Centralizes all cryptographic capabilities for OIDC, split by role:
/// - Idp: when this framework acts as an OpenID Connect Identity Provider.
/// - Client: when this framework acts as an OpenID Connect Client.
/// 
/// IMPORTANT:
/// - Do NOT share sets between IdP and Client. They may diverge when using an external IdP.
/// - This class is the single source of truth for what algorithms we are willing to use/accept.
/// </summary>
public static class OIDCCryptoCapabilities
{
    /// <summary>
    /// Capabilities when this framework acts as the Identity Provider.
    /// These values are used to build the IdP metadata (well-known) and to
    /// decide what the IdP can do with its own keys and with client keys.
    /// </summary>
    public static class Idp
    {
        /// <summary>
        /// Signing algorithms the IdP can use with an RSA certificate for ID tokens / UserInfo.
        /// This is what we advertise in id_token_signing_alg_values_supported, etc.,
        /// when the IdP certificate has an RSA key.
        /// </summary>
        public static readonly IReadOnlyList<OIDCSigningAlg> RsaSigningAlgs =
        [
            OIDCSigningAlg.RS256,
            OIDCSigningAlg.RS384,
            OIDCSigningAlg.RS512,
            OIDCSigningAlg.PS256,
            OIDCSigningAlg.PS384,
            OIDCSigningAlg.PS512
        ];

        /// <summary>
        /// Signing algorithms the IdP can use with an EC certificate for ID tokens / UserInfo.
        /// This is what we advertise when the IdP certificate has an EC key.
        /// </summary>
        public static readonly IReadOnlyList<OIDCSigningAlg> EcSigningAlgs =
        [
            OIDCSigningAlg.ES256,
            OIDCSigningAlg.ES384,
            OIDCSigningAlg.ES512
        ];

        /// <summary>
        /// Signing algorithms that the IdP is willing to ACCEPT from clients
        /// for:
        /// - private_key_jwt client authentication
        /// - signed request objects
        /// This represents what the IdP implementation can verify against client JWKS.
        /// Currently we only support RSA-based client keys.
        /// </summary>
        public static readonly IReadOnlyList<OIDCSigningAlg> AcceptedClientAssertionSigningAlgs =
        [
            OIDCSigningAlg.RS256,
            OIDCSigningAlg.RS384,
            OIDCSigningAlg.RS512,
            OIDCSigningAlg.PS256,
            OIDCSigningAlg.PS384,
            OIDCSigningAlg.PS512
            // ES* intentionally not advertised for clients for now.
        ];

        /// <summary>
        /// Key management algorithms that clients can use when encrypting TO the IdP
        /// (request objects, etc.) using the IdP RSA "enc" keys from the IdP JWKS.
        /// </summary>
        public static readonly IReadOnlyList<OIDCKeyEncryptionAlgorithm> RsaKeyManagementAlgsForRequestObjects =
        [
            OIDCKeyEncryptionAlgorithm.RSA_OAEP
            // RSA1_5 intentionally disabled: weaker and harder to implement safely.
        ];

        /// <summary>
        /// Key management algorithms that clients can use when encrypting TO the IdP
        /// with EC keys. Present for future support; currently not used while we only
        /// deploy RSA certs.
        /// </summary>
        public static readonly IReadOnlyList<OIDCKeyEncryptionAlgorithm> EcKeyManagementAlgsForRequestObjects =
        [
            OIDCKeyEncryptionAlgorithm.ECDH_ES_A256KW,
            OIDCKeyEncryptionAlgorithm.ECDH_ES
        ];

        /// <summary>
        /// Key management algorithms that the IdP can use when encrypting ID tokens TO clients,
        /// using client "enc" keys from the client JWKS.
        /// This is what we advertise in id_token_encryption_alg_values_supported.
        /// </summary>
        public static readonly IReadOnlyList<OIDCKeyEncryptionAlgorithm> IdTokenKeyManagementAlgsForEncryptingToClients =
        [
            OIDCKeyEncryptionAlgorithm.RSA_OAEP
            // ECDH_* not enabled yet while we only use RSA certs in clients.
        ];

        /// <summary>
        /// Content encryption algorithms ("enc") for JWE that the IdP can handle.
        /// Used for both ID tokens and encrypted request objects.
        /// </summary>
        public static readonly IReadOnlyList<OIDCEncryptionAlg> JweContentEncryptionAlgs =
        [
            OIDCEncryptionAlg.A256GCM,
            OIDCEncryptionAlg.A192GCM,
            OIDCEncryptionAlg.A128GCM
            // CBC-HS* intentionally not used to keep the surface smaller and simpler.
        ];

        /// <summary>
        /// Allowed JWE "alg" for request objects encrypted TO our IdP.
        /// This must match what we advertise in:
        /// request_object_encryption_alg_values_supported
        /// for the IdP role.
        /// </summary>
        public static readonly IReadOnlySet<string> AllowedRequestObjectKeyManagementAlgStrings =
            RsaKeyManagementAlgsForRequestObjects
                .Concat(EcKeyManagementAlgsForRequestObjects)
                .Select(a => a.ToAlgString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Allowed JWE "enc" for request objects encrypted TO our IdP.
        /// This must match what we advertise in:
        /// request_object_encryption_enc_values_supported
        /// </summary>  
        public static readonly IReadOnlySet<string> AllowedRequestObjectContentEncryptionAlgStrings =
            JweContentEncryptionAlgs
                .Select(a => a.ToAlgString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);


        public static readonly HashSet<string> AllowedClientAssertionSigningAlgStrings = AcceptedClientAssertionSigningAlgs
                .Select(a => a.ToString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

    }

    /// <summary>
    /// Capabilities when this framework acts as an OIDC Client.
    /// These values represent what we accept from an (possibly external) IdP.
    /// They MUST NOT be automatically kept identical to Idp.*; the external IdP
    /// may support a larger or different set of algorithms than we are willing to accept.
    /// </summary>
    public static class Client
    {
        /// <summary>
        /// Allowed signing algorithms for tokens received from an IdP (id_token, etc.).
        /// Used as a hard allow-list when validating signatures.
        /// </summary>
        public static readonly HashSet<string> AllowedIdTokenSigningAlgorithms = new(StringComparer.Ordinal)
        {
            SecurityAlgorithms.RsaSha256,
            SecurityAlgorithms.RsaSha384,
            SecurityAlgorithms.RsaSha512,
            SecurityAlgorithms.RsaSsaPssSha256,
            SecurityAlgorithms.RsaSsaPssSha384,
            SecurityAlgorithms.RsaSsaPssSha512
            // ECDSA not yet supported on the client side.
        };

        /// <summary>
        /// Allowed JWE key management algorithms for encrypted id_tokens received from an IdP.
        /// </summary>
        public static readonly HashSet<string> AllowedIdTokenKeyManagementAlgs = new(StringComparer.Ordinal)
        {
            SecurityAlgorithms.RsaOAEP
            // ECDH not yet supported.
        };

        /// <summary>
        /// Allowed JWE content encryption algorithms for encrypted id_tokens received from an IdP.
        /// </summary>
        public static readonly HashSet<string> AllowedIdTokenContentEncryptionAlgs = new(StringComparer.Ordinal)
        {
            SecurityAlgorithms.Aes256Gcm,
            SecurityAlgorithms.Aes192Gcm,
            SecurityAlgorithms.Aes128Gcm
        };

        /// <summary>
        /// OIDC signing algorithms we can use when acting as a client:
        /// - client_assertion for private_key_jwt
        /// - signed request objects
        /// This is expressed in OIDC enums instead of SecurityAlgorithms constants.
        /// </summary>
        public static readonly IReadOnlyList<OIDCSigningAlg> RequestObjectAndClientAssertionSigningAlgs =
        [
            OIDCSigningAlg.RS256,
            OIDCSigningAlg.RS384,
            OIDCSigningAlg.RS512,
            OIDCSigningAlg.PS256,
            OIDCSigningAlg.PS384,
            OIDCSigningAlg.PS512
            // ES* can be added here when ECDSA client certificates are supported.
        ];
    }
}
