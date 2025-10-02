using MicroM.Configuration;

namespace MicroM.Web.Authentication.SSO;

public static class WellKnownProvider
{

    public static OIDCWellKnownResponse CreateWellKnown(ApplicationOption app, string request_base)
    {
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

            // Capabilities
            response_types_supported: [OIDCResponseType.code],
            response_modes_supported: [OIDCResponseMode.query, OIDCResponseMode.form_post],
            subject_types_supported: [OIDCSubjectType.@public],
            id_token_signing_alg_values_supported: [OIDCSigningAlg.RS256, app.OIDCTokenSigningAlg!.Value],
            code_challenge_methods_supported: [app.OIDCTokenCodeChallengeMethod!.Value],
            grant_types_supported: [OIDCGrantType.authorization_code, OIDCGrantType.refresh_token],
            backchannel_logout_supported: true,
            backchannel_logout_session_supported: true,

            require_pushed_authorization_requests: true,
            request_uri_parameter_supported: true,
            authorization_response_iss_parameter_supported: true,

            introspection_endpoint_auth_methods_supported: [OIDCTokenEndpointAuthMethod.private_key_jwt],
            introspection_endpoint_auth_signing_alg_values_supported: [OIDCSigningAlg.RS256, app.OIDCTokenSigningAlg!.Value],

            revocation_endpoint_auth_methods_supported: [OIDCTokenEndpointAuthMethod.private_key_jwt],
            revocation_endpoint_auth_signing_alg_values_supported: [OIDCSigningAlg.RS256, app.OIDCTokenSigningAlg!.Value],

            // Token endpoint
            token_endpoint_auth_methods_supported: [OIDCTokenEndpointAuthMethod.private_key_jwt],
            token_endpoint_auth_signing_alg_values_supported: [OIDCSigningAlg.RS256, app.OIDCTokenSigningAlg!.Value],

            scopes_supported: [OIDCProfileScopes.openid, OIDCProfileScopes.profile, OIDCProfileScopes.email]
        );
    }

}
