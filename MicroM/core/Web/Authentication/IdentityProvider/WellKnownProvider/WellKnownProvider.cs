using MicroM.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace MicroM.Web.Authentication.SSO;

public static class WellKnownProvider
{
    // Decide which signing algorithms the IdP can use with its own keys (for ID token / UserInfo signing)
    // based on the IdP certificate key type (RSA vs EC).
    private static IReadOnlyList<OIDCSigningAlg> BuildIdPSigningAlgList(X509Certificate2 cert)
    {
        if (cert.GetRSAPrivateKey() != null)
            return OIDCCryptoCapabilities.Idp.RsaSigningAlgs;

        if (cert.GetECDsaPrivateKey() != null)
            return OIDCCryptoCapabilities.Idp.EcSigningAlgs;

        return [];
    }

    // Decide which key management algorithms clients can use to encrypt TO the IdP,
    // based on the IdP key type published in the IdP JWKS.
    private static IReadOnlyList<OIDCKeyEncryptionAlgorithm> BuildIdPKeyEncryptionAlgList(X509Certificate2 cert)
    {
        if (cert.GetRSAPrivateKey() != null)
            return OIDCCryptoCapabilities.Idp.RsaKeyManagementAlgsForRequestObjects;

        if (cert.GetECDsaPrivateKey() != null)
            return OIDCCryptoCapabilities.Idp.EcKeyManagementAlgsForRequestObjects;

        return [];
    }

    private static List<OIDCCodeChallengeMethod> BuildPkceList(ApplicationOption app)
    {
        return app.OIDCAllowPkcePlain ? [OIDCCodeChallengeMethod.S256, OIDCCodeChallengeMethod.plain] : [OIDCCodeChallengeMethod.S256];
    }


    public static OIDCWellKnownResponse CreateWellKnown(ApplicationOption app, string request_base, X509Certificate2 cert)
    {
        // Algorithms where the IdP is the signer (uses its own keys / IdP JWKS "sig" keys)
        var idpSigningAlgs = BuildIdPSigningAlgList(cert);

        // Algorithms where the IdP is the recipient of encrypted data (clients encrypt TO the IdP).
        // This depends on the IdP certificate / IdP JWKS "enc" keys.
        var idpKeyEncryptionAlgs = BuildIdPKeyEncryptionAlgList(cert);

        // Algorithms where the IdP encrypts TO the client using the client's JWKS ("enc" keys).
        // This is a fixed capability list, independent of the IdP certificate.
        var idTokenKeyEncryptionAlgs = OIDCCryptoCapabilities.Idp.IdTokenKeyManagementAlgsForEncryptingToClients;

        var pkceMethods = BuildPkceList(app);
        var contentEncryptionAlgs = OIDCCryptoCapabilities.Idp.JweContentEncryptionAlgs;


        return new OIDCWellKnownResponse
        (
            issuer: request_base,
            authorization_endpoint: $"{request_base}/oauth2/authorize",
            token_endpoint: $"{request_base}/oauth2/token",
            jwks_uri: $"{request_base}/oidc/jwks",
            pushed_authorization_request_endpoint: $"{request_base}/oauth2/par",
            end_session_endpoint: $"{request_base}/oauth2/endsession",

            // userinfo, revocation and introspection are not mandatoiry. Not implemented

            // userinfo_endpoint: $"{request_base}/oauth2/userinfo",
            // revocation_endpoint: $"{request_base}/oauth2/revoke",
            // introspection_endpoint: $"{request_base}/oauth2/introspect",

            // revocation_endpoint_auth_signing_alg_values_supported: OIDCCryptoCapabilities.Idp.AcceptedClientAssertionSigningAlgs,
            // introspection_endpoint_auth_signing_alg_values_supported: OIDCCryptoCapabilities.Idp.AcceptedClientAssertionSigningAlgs,
            // revocation_endpoint_auth_methods_supported: [OIDCTokenEndpointAuthMethod.private_key_jwt],
            // introspection_endpoint_auth_methods_supported: [OIDCTokenEndpointAuthMethod.private_key_jwt],


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
            token_endpoint_auth_signing_alg_values_supported: OIDCCryptoCapabilities.Idp.AcceptedClientAssertionSigningAlgs,
            request_object_signing_alg_values_supported: OIDCCryptoCapabilities.Idp.AcceptedClientAssertionSigningAlgs,


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
