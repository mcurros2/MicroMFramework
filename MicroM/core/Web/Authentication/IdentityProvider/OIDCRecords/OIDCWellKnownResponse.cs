using System.Text.Json.Serialization;

namespace MicroM.Web.Authentication.SSO;

// String-style enums: member names match the exact strings expected in the OIDC discovery document.
// JsonStringEnumConverter will serialize enum members by name (so keep these identifiers matching spec strings).
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCResponseType { code }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCResponseMode { query, fragment, form_post }


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCSubjectType { @public, pairwise }

/*
   +--------------+-------------------------------+--------------------+
   | "alg" Param  | Digital Signature or MAC      | Implementation     |
   | Value        | Algorithm                     | Requirements       |
   +--------------+-------------------------------+--------------------+
   | HS256        | HMAC using SHA-256            | Required           |
   | HS384        | HMAC using SHA-384            | Optional           |
   | HS512        | HMAC using SHA-512            | Optional           |
   | RS256        | RSASSA-PKCS1-v1_5 using       | Recommended        |
   |              | SHA-256                       |                    |
   | RS384        | RSASSA-PKCS1-v1_5 using       | Optional           |
   |              | SHA-384                       |                    |
   | RS512        | RSASSA-PKCS1-v1_5 using       | Optional           |
   |              | SHA-512                       |                    |
   | ES256        | ECDSA using P-256 and SHA-256 | Recommended+       |
   | ES384        | ECDSA using P-384 and SHA-384 | Optional           |
   | ES512        | ECDSA using P-521 and SHA-512 | Optional           |
   | PS256        | RSASSA-PSS using SHA-256 and  | Optional           |
   |              | MGF1 with SHA-256             |                    |
   | PS384        | RSASSA-PSS using SHA-384 and  | Optional           |
   |              | MGF1 with SHA-384             |                    |
   | PS512        | RSASSA-PSS using SHA-512 and  | Optional           |
   |              | MGF1 with SHA-512             |                    |
   | none         | No digital signature or MAC   | Optional           |
   |              | performed                     |                    |
   +--------------+-------------------------------+--------------------+
 
 */
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCSigningAlg { HS256, HS384, HS512, RS256, RS384, RS512, ES256, ES384, ES512, PS256, PS384, PS512, none }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCCodeChallengeMethod { S256 }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCKeyType { RSA, EC, OKP }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCKeyUse { sig, enc }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCKeyCurveValues
{
    [JsonPropertyName("P-256")]
    P256,
    [JsonPropertyName("P-384")]
    P384,
    [JsonPropertyName("P-521")]
    P521,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCGrantType { authorization_code, refresh_token }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCTokenEndpointAuthMethod { client_secret_basic, private_key_jwt }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCProfileScopes { openid, email, profile }

public sealed record OIDCJwksResponse(List<OIDCJwksKeyResponse> keys);

public sealed record OIDCWellKnownResponse
(
    // Required
    string issuer,
    string authorization_endpoint,
    string token_endpoint,
    string jwks_uri,

    // Use strongly-typed lists so serialization follows spec and values are consistent.
    List<OIDCResponseType>? response_types_supported = null,
    List<OIDCSubjectType>? subject_types_supported = null,
    List<OIDCSigningAlg>? id_token_signing_alg_values_supported = null,

    // Optional
    string? userinfo_endpoint = null,
    string? end_session_endpoint = null,
    string? pushed_authorization_request_endpoint = null,
    string? check_session_iframe = null,
    List<OIDCGrantType>? grant_types_supported = null,
    List<string>? acr_values_supported = null,
    List<OIDCResponseMode>? response_modes_supported = null,
    List<OIDCProfileScopes>? scopes_supported = null,
    List<string>? claims_supported = null,
    bool? claims_parameter_supported = null,
    bool? request_uri_parameter_supported = null,
    bool? require_request_uri_registration = null,
    bool? require_pushed_authorization_requests = null,

    bool? frontchannel_logout_supported = null,
    bool? frontchannel_logout_session_supported = null,
    bool? backchannel_logout_supported = null,
    bool? backchannel_logout_session_supported = null,

    // Token auth methods / signing algs
    List<OIDCTokenEndpointAuthMethod>? token_endpoint_auth_methods_supported = null,
    List<OIDCSigningAlg>? token_endpoint_auth_signing_alg_values_supported = null,

    List<string>? display_values_supported = null,
    List<string>? claim_types_supported = null,
    string? service_documentation = null,
    string? op_policy_uri = null,
    string? op_tos_uri = null,
    List<string>? request_object_signing_alg_values_supported = null,
    List<string>? ui_locales_supported = null,
    List<string>? id_token_encryption_alg_values_supported = null,
    List<string>? id_token_encryption_enc_values_supported = null,
    List<string>? userinfo_signing_alg_values_supported = null,
    List<string>? userinfo_encryption_alg_values_supported = null,
    List<string>? userinfo_encryption_enc_values_supported = null,
    List<string>? request_object_encryption_alg_values_supported = null,
    List<string>? request_object_encryption_enc_values_supported = null,

    string? revocation_endpoint = null,
    List<OIDCTokenEndpointAuthMethod>? revocation_endpoint_auth_methods_supported = null,
    List<OIDCSigningAlg>? revocation_endpoint_auth_signing_alg_values_supported = null,

    string? introspection_endpoint = null,
    List<OIDCTokenEndpointAuthMethod>? introspection_endpoint_auth_methods_supported = null,
    List<OIDCSigningAlg>? introspection_endpoint_auth_signing_alg_values_supported = null,

    bool? authorization_response_iss_parameter_supported = null,

    List<OIDCCodeChallengeMethod>? code_challenge_methods_supported = null,
    string? registration_endpoint = null,
    List<string>? claims_locales_supported = null,
    bool? request_parameter_supported = null
);