using Microsoft.IdentityModel.Tokens;

namespace MicroM.Web.Authentication.SSO;

public static class OIDCRecordExtensions
{
    public static string ToAlgString(this OIDCKeyEncryptionAlgorithm alg) => alg switch
    {
        OIDCKeyEncryptionAlgorithm.RSA_OAEP => SecurityAlgorithms.RsaOAEP,
        OIDCKeyEncryptionAlgorithm.RSA_OAEP_256 => "RSA-OAEP-256",
        OIDCKeyEncryptionAlgorithm.ECDH_ES => SecurityAlgorithms.EcdhEs,
        OIDCKeyEncryptionAlgorithm.ECDH_ES_A128KW => SecurityAlgorithms.EcdhEsA128kw,
        OIDCKeyEncryptionAlgorithm.ECDH_ES_A192KW => SecurityAlgorithms.EcdhEsA192kw,
        OIDCKeyEncryptionAlgorithm.ECDH_ES_A256KW => SecurityAlgorithms.EcdhEsA256kw,
        _ => alg.ToString()
    };

    public static string ToAlgString(this OIDCEncryptionAlg alg) => alg switch
    {
        OIDCEncryptionAlg.A128CBC_HS256 => SecurityAlgorithms.Aes128CbcHmacSha256,
        OIDCEncryptionAlg.A192CBC_HS384 => SecurityAlgorithms.Aes192CbcHmacSha384,
        OIDCEncryptionAlg.A256CBC_HS512 => SecurityAlgorithms.Aes256CbcHmacSha512,
        _ => alg.ToString()
    };
}
