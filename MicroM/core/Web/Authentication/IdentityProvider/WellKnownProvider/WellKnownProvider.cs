using MicroM.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace MicroM.Web.Authentication.SSO;

public static class WellKnownProvider
{
    private static List<OIDCSigningAlg> BuildSigningAlgList(ApplicationOption app, X509Certificate2? cert)
    {
        var list = new HashSet<OIDCSigningAlg>();

        if (cert != null)
        {
            if (cert.GetECDsaPrivateKey() != null)
            {
                list.Add(OIDCSigningAlg.ES256);
                list.Add(OIDCSigningAlg.ES384);
                list.Add(OIDCSigningAlg.ES512);
            }
            else if (cert.GetRSAPrivateKey() != null)
            {
                list.Add(OIDCSigningAlg.RS256);
                list.Add(OIDCSigningAlg.RS384);
                list.Add(OIDCSigningAlg.RS512);
                list.Add(OIDCSigningAlg.PS256);
                list.Add(OIDCSigningAlg.PS384);
                list.Add(OIDCSigningAlg.PS512);
            }
        }

        if (app.OIDCTokenSigningAlg.HasValue)
            list.Add(app.OIDCTokenSigningAlg.Value);

        var ordered = list
            .OrderBy(a => a switch
            {
                OIDCSigningAlg.ES256 => 0,
                OIDCSigningAlg.ES384 => 1,
                OIDCSigningAlg.ES512 => 2,
                OIDCSigningAlg.PS256 => 3,
                OIDCSigningAlg.PS384 => 4,
                OIDCSigningAlg.PS512 => 5,
                OIDCSigningAlg.RS256 => 6,
                OIDCSigningAlg.RS384 => 7,
                OIDCSigningAlg.RS512 => 8,
                OIDCSigningAlg.HS256 => 20,
                OIDCSigningAlg.HS384 => 21,
                OIDCSigningAlg.HS512 => 22,
                OIDCSigningAlg.none => 50,
                _ => 99
            })
            .ToList();

        return ordered;
    }

    // OP-supported encryption key algorithms for id_token (independent of IdP cert)
    private static List<OIDCKeyEncryptionAlgorithm> BuildSupportedIdTokenKeyAlgs()
        => new()
        {
            OIDCKeyEncryptionAlgorithm.RSA_OAEP,
            OIDCKeyEncryptionAlgorithm.ECDH_ES_A256KW,
            OIDCKeyEncryptionAlgorithm.ECDH_ES,
            // RSA1_5 allowed for legacy interop; keep last
            OIDCKeyEncryptionAlgorithm.RSA1_5
        };

    private static List<OIDCEncryptionAlg> BuildEncryptionEcnList(ApplicationOption app) =>
    [
        OIDCEncryptionAlg.A256GCM,
        OIDCEncryptionAlg.A256CBC_HS512,
        OIDCEncryptionAlg.A192GCM,
        OIDCEncryptionAlg.A192CBC_HS384,
        OIDCEncryptionAlg.A128GCM,
        OIDCEncryptionAlg.A128CBC_HS256
    ];

    private static List<OIDCCodeChallengeMethod> BuildPkceList(ApplicationOption app)
    {
        var list = new List<OIDCCodeChallengeMethod> { OIDCCodeChallengeMethod.S256 };
        if (app.OIDCAllowPkcePlain) list.Add(OIDCCodeChallengeMethod.plain);
        return list;
    }

    // Accepted algorithms for private_key_jwt client_assertion (token/revocation/introspection endpoints)
    private static List<OIDCSigningAlg> BuildAcceptedClientAssertionAlgs()
        => new()
        {
            OIDCSigningAlg.RS256, OIDCSigningAlg.RS384, OIDCSigningAlg.RS512,
            OIDCSigningAlg.PS256, OIDCSigningAlg.PS384, OIDCSigningAlg.PS512,
            OIDCSigningAlg.ES256, OIDCSigningAlg.ES384, OIDCSigningAlg.ES512
        };

    public static OIDCWellKnownResponse CreateWellKnown(ApplicationOption app, string request_base, X509Certificate2? cert)
    {
        var signingAlgs = BuildSigningAlgList(app, cert);
        var pkceMethods = BuildPkceList(app);
        var idTokenKeyAlgs = BuildSupportedIdTokenKeyAlgs();
        var contentEncs = BuildEncryptionEcnList(app);
        var clientAssertionAlgs = BuildAcceptedClientAssertionAlgs();

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

            // Encryption support is Client capability-based, not tied to IdP cert
            id_token_encryption_alg_values_supported: idTokenKeyAlgs,
            id_token_encryption_enc_values_supported: contentEncs,

            id_token_signing_alg_values_supported: signingAlgs,

            // Accepted algs for private_key_jwt client_assertion
            token_endpoint_auth_signing_alg_values_supported: clientAssertionAlgs,
            revocation_endpoint_auth_signing_alg_values_supported: clientAssertionAlgs,
            introspection_endpoint_auth_signing_alg_values_supported: clientAssertionAlgs,

            userinfo_signing_alg_values_supported: signingAlgs,

            code_challenge_methods_supported: pkceMethods,

            grant_types_supported: [OIDCGrantType.authorization_code, OIDCGrantType.refresh_token],
            backchannel_logout_supported: true,
            backchannel_logout_session_supported: true,

            require_pushed_authorization_requests: true,
            request_uri_parameter_supported: true,
            authorization_response_iss_parameter_supported: true,

            introspection_endpoint_auth_methods_supported: [OIDCTokenEndpointAuthMethod.private_key_jwt],
            revocation_endpoint_auth_methods_supported: [OIDCTokenEndpointAuthMethod.private_key_jwt],
            token_endpoint_auth_methods_supported: [OIDCTokenEndpointAuthMethod.private_key_jwt],

            scopes_supported: [OIDCProfileScopes.openid, OIDCProfileScopes.profile, OIDCProfileScopes.email]
        );
    }

}
