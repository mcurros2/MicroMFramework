using MicroM.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace MicroM.Web.Authentication.SSO;

public static class WellKnownProvider
{
    // Signing algorithms that the IdP can use with its own keys (depending on the IdP key type).
    // These are used when the IdP signs ID tokens or UserInfo JWTs.
    private static readonly List<OIDCSigningAlg> ECDSigningAlgList =
    [
        OIDCSigningAlg.ES256,
        OIDCSigningAlg.ES384,
        OIDCSigningAlg.ES512
    ];

    private static readonly List<OIDCSigningAlg> RSASigningAlgList =
    [
        OIDCSigningAlg.RS256, OIDCSigningAlg.RS384, OIDCSigningAlg.RS512,
        OIDCSigningAlg.PS256, OIDCSigningAlg.PS384, OIDCSigningAlg.PS512
    ];


    // Signing algorithms that the IdP is willing to ACCEPT from clients
    // (for request objects and private_key_jwt client authentication).
    // This does NOT depend on the IdP certificate; it depends on what the IdP implementation (.NET 8 + JOSE lib)
    // can verify against client keys published in the client's JWKS.
    private static readonly List<OIDCSigningAlg> ClientSigningAlgList =
    [
        ..RSASigningAlgList,
        // intentionally disabled for now - we only use RSA keys for clients
        //..ECDSigningAlgList
    ];

    // Key management algorithms that clients can use when encrypting TO the IdP,
    // using the IdP's public key published in the IdP JWKS (use: "enc").
    // This depends on the IdP key type (RSA vs EC).
    private static readonly List<OIDCKeyEncryptionAlgorithm> ECDKeyEncryptionAlgs =
    [
        OIDCKeyEncryptionAlgorithm.ECDH_ES_A256KW,
        OIDCKeyEncryptionAlgorithm.ECDH_ES,
    ];

    private static readonly List<OIDCKeyEncryptionAlgorithm> RSAKeyEncryptionAlgs =
    [
        OIDCKeyEncryptionAlgorithm.RSA_OAEP,

        // Intentionally disabled for now - RSA1_5 is less secure and more complex to implement correctly
        //OIDCKeyEncryptionAlgorithm.RSA1_5
    ];

    // Content encryption algorithms ("enc") for JWE.
    // These do NOT depend on the IdP certificate or key type.
    // They must reflect what the IdP implementation can actually handle.

    private static readonly List<OIDCEncryptionAlg> ContentEncryptionAlgs =
    [
        OIDCEncryptionAlg.A256GCM,
        OIDCEncryptionAlg.A192GCM,
        OIDCEncryptionAlg.A128GCM,

        // Intentionally disabled for now - CBC mode is less secure and more complex to implement correctly
        //OIDCEncryptionAlg.A256CBC_HS512,
        //OIDCEncryptionAlg.A192CBC_HS384,
        //OIDCEncryptionAlg.A128CBC_HS256
    ];

    // Key management algorithms that the IdP can use when encrypting ID tokens TO clients.
    // This does NOT depend on the IdP certificate, because encryption is done with client keys
    // obtained from the client's JWKS (use: "enc").
    // The list should represent all JWE "alg" values that the IdP supports when encrypting ID tokens.

    private static readonly List<OIDCKeyEncryptionAlgorithm> IdTokenKeyEncryptionAlgs =
    [
        ..RSAKeyEncryptionAlgs,
        // We are using RSA certificates for now, so ECDH is not yet supported
        //..ECDKeyEncryptionAlgs,
    ];

    // Decide which signing algorithms the IdP can use with its own keys (for ID token / UserInfo signing)
    // based on the IdP certificate key type (RSA vs EC).
    private static List<OIDCSigningAlg> BuildIdPSigningAlgList(ApplicationOption app, X509Certificate2 cert)
    {
        if (cert.GetRSAPrivateKey() != null) return RSASigningAlgList;
        else if (cert.GetECDsaPrivateKey() != null) return ECDSigningAlgList;
        return [];
    }

    // Decide which key management algorithms clients can use to encrypt TO the IdP,
    // based on the IdP key type published in the IdP JWKS.
    private static List<OIDCKeyEncryptionAlgorithm> BuildIdPKeyEncryptionAlgList(ApplicationOption app, X509Certificate2 cert)
    {
        if (cert.GetRSAPrivateKey() != null) return RSAKeyEncryptionAlgs;
        else if (cert.GetECDsaPrivateKey() != null) return ECDKeyEncryptionAlgs;
        return [];
    }

    private static List<OIDCCodeChallengeMethod> BuildPkceList(ApplicationOption app)
    {
        return app.OIDCAllowPkcePlain ? [OIDCCodeChallengeMethod.S256, OIDCCodeChallengeMethod.plain] : [OIDCCodeChallengeMethod.S256];
    }

    public static OIDCWellKnownResponse CreateWellKnown(ApplicationOption app, string request_base, X509Certificate2 cert)
    {
        // Algorithms where the IdP is the signer (uses its own keys / IdP JWKS "sig" keys)
        var idpSigningAlgs = BuildIdPSigningAlgList(app, cert);

        // Algorithms where the IdP is the recipient of encrypted data (clients encrypt TO the IdP).
        // This depends on the IdP certificate / IdP JWKS "enc" keys.
        var idpKeyEncryptionAlgs = BuildIdPKeyEncryptionAlgList(app, cert);

        // Algorithms where the IdP encrypts TO the client using the client's JWKS ("enc" keys).
        // This is a fixed capability list, independent of the IdP certificate.
        var idTokenKeyEncryptionAlgs = IdTokenKeyEncryptionAlgs;

        var pkceMethods = BuildPkceList(app);
        var contentEncryptionAlgs = ContentEncryptionAlgs;


        return new OIDCWellKnownResponse
        (
            issuer: request_base,
            authorization_endpoint: $"{request_base}/oauth2/authorize",
            token_endpoint: $"{request_base}/oauth2/token",
            userinfo_endpoint: $"{request_base}/oauth2/userinfo",
            jwks_uri: $"{request_base}/oidc/jwks",
            pushed_authorization_request_endpoint: $"{request_base}/oauth2/par",
            end_session_endpoint: $"{request_base}/oauth2/endsession",
            revocation_endpoint: $"{request_base}/oauth2/revoke",
            introspection_endpoint: $"{request_base}/oauth2/introspect",

            response_types_supported: [OIDCResponseType.code],
            response_modes_supported: [OIDCResponseMode.query, OIDCResponseMode.form_post],
            subject_types_supported: [OIDCSubjectType.@public, OIDCSubjectType.pairwise],

            // Signing:
            //  - These fields describe algorithms the IdP uses when it SIGNS tokens/claims
            //    with its own keys (IdP JWKS "sig" keys).
            id_token_signing_alg_values_supported: idpSigningAlgs,
            userinfo_signing_alg_values_supported: idpSigningAlgs,

            //  - The following fields describe algorithms the IdP ACCEPTS from clients:
            //    - For private_key_jwt client authentication (client keys from client JWKS "sig").
            //    - For signed Request Objects.
            //    They do NOT depend on the IdP certificate; they are a fixed capability list.
            token_endpoint_auth_signing_alg_values_supported: ClientSigningAlgList,
            revocation_endpoint_auth_signing_alg_values_supported: ClientSigningAlgList,
            introspection_endpoint_auth_signing_alg_values_supported: ClientSigningAlgList,
            request_object_signing_alg_values_supported: ClientSigningAlgList,


            // Encryption:
            //  - "alg" for ID tokens: IdP encrypts TO the client, using client JWKS "enc" keys.
            //    This does NOT depend on the IdP certificate; this is a fixed capability list.
            id_token_encryption_alg_values_supported: idTokenKeyEncryptionAlgs,

            //  - "enc" for ID tokens: content encryption algorithms the IdP can use when encrypting ID tokens.
            //    Also a fixed capability list.
            id_token_encryption_enc_values_supported: contentEncryptionAlgs,

            //  - "alg" for Request Objects: clients encrypt TO the IdP, using IdP JWKS "enc" keys.
            //    This MUST be compatible with the IdP key type (RSA vs EC), so it depends on the IdP certificate.
            request_object_encryption_alg_values_supported: idpKeyEncryptionAlgs,

            //  - "enc" for Request Objects: content encryption algorithms for Request Objects (fixed list).
            request_object_encryption_enc_values_supported: contentEncryptionAlgs,


            // PKCE
            code_challenge_methods_supported: pkceMethods,

            // Grant types & auth methods
            grant_types_supported: [OIDCGrantType.authorization_code, OIDCGrantType.refresh_token],
            token_endpoint_auth_methods_supported: [OIDCTokenEndpointAuthMethod.private_key_jwt],
            revocation_endpoint_auth_methods_supported: [OIDCTokenEndpointAuthMethod.private_key_jwt],
            introspection_endpoint_auth_methods_supported: [OIDCTokenEndpointAuthMethod.private_key_jwt],

            scopes_supported: [OIDCProfileScopes.openid, OIDCProfileScopes.profile, OIDCProfileScopes.email],

            // PAR & security
            require_pushed_authorization_requests: true,
            request_uri_parameter_supported: false,
            authorization_response_iss_parameter_supported: true,
            backchannel_logout_supported: true,
            backchannel_logout_session_supported: true
        );
    }

}
