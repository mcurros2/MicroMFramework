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
                OIDCSigningAlg.ES512 => 0,
                OIDCSigningAlg.ES384 => 1,
                OIDCSigningAlg.ES256 => 2,
                OIDCSigningAlg.PS512 => 3,
                OIDCSigningAlg.PS384 => 4,
                OIDCSigningAlg.PS256 => 5,
                OIDCSigningAlg.RS512 => 6,
                OIDCSigningAlg.RS384 => 7,
                OIDCSigningAlg.RS256 => 8,
                OIDCSigningAlg.HS512 => 9,
                OIDCSigningAlg.HS384 => 10,
                OIDCSigningAlg.HS256 => 11,
                OIDCSigningAlg.none => 12,
                _ => 50
            })
            .ToList();

        return ordered;
    }

    private static List<OIDCKeyEncryptionAlgorithm> BuildEncryptionAlgList(ApplicationOption app, X509Certificate2? cert)
    {
        var list = new HashSet<OIDCKeyEncryptionAlgorithm>();

        // Only asymmetric encryption is supported for OIDC (no shared-secret/dir/AES-KW).
        if (cert != null)
        {
            if (cert.GetRSAPublicKey() != null)
            {
                // NOTE: Removed RSA_OAEP_256; not supported by current Microsoft.IdentityModel.Tokens version (.NET 8 target).
                list.Add(OIDCKeyEncryptionAlgorithm.RSA_OAEP);
                list.Add(OIDCKeyEncryptionAlgorithm.RSA1_5);
            }
            if (cert.GetECDsaPublicKey() != null)
            {
                list.Add(OIDCKeyEncryptionAlgorithm.ECDH_ES_A256KW);
                list.Add(OIDCKeyEncryptionAlgorithm.ECDH_ES);
            }
        }

        var ordered = list
            .OrderBy(a => a switch
            {
                OIDCKeyEncryptionAlgorithm.RSA_OAEP => 0,
                OIDCKeyEncryptionAlgorithm.ECDH_ES_A256KW => 1,
                OIDCKeyEncryptionAlgorithm.ECDH_ES => 2,
                OIDCKeyEncryptionAlgorithm.RSA1_5 => 6,
                _ => 50
            })
            .ToList();

        return ordered;
    }

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

    public static OIDCWellKnownResponse CreateWellKnown(ApplicationOption app, string request_base, X509Certificate2? cert)
    {
        var signingAlgs = BuildSigningAlgList(app, cert);
        var pkceMethods = BuildPkceList(app);
        var keyEncAlgs = BuildEncryptionAlgList(app, cert);
        var contentEncs = BuildEncryptionEcnList(app);

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

            id_token_encryption_alg_values_supported: keyEncAlgs.Count > 0 ? keyEncAlgs : null,
            id_token_encryption_enc_values_supported: contentEncs,

            id_token_signing_alg_values_supported: signingAlgs,
            token_endpoint_auth_signing_alg_values_supported: signingAlgs,
            revocation_endpoint_auth_signing_alg_values_supported: signingAlgs,
            introspection_endpoint_auth_signing_alg_values_supported: signingAlgs,
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
