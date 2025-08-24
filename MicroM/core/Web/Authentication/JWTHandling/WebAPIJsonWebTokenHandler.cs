using MicroM.Configuration;
using MicroM.Core;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace MicroM.Web.Authentication;

/// <summary>
/// Encapsulates the result of generating a JSON Web Token.
/// </summary>
public record TokenResult
{
    /// <summary>
    /// The serialized JSON Web Token.
    /// </summary>
    public string? Token { get; init; }

    /// <summary>
    /// Descriptor used to create the token.
    /// </summary>
    public SecurityTokenDescriptor? SD { get; init; }
}

/// <summary>
/// Handles creation and validation of encrypted JWTs for MicroM Web APIs.
/// </summary>
/// <param name="app_config">Application configuration provider used to resolve JWT settings.</param>
/// <param name="http_context">Accessor for the current HTTP context.</param>
/// <param name="logger">Logger for diagnostic messages.</param>
public class WebAPIJsonWebTokenHandler(
    IMicroMAppConfiguration app_config, IHttpContextAccessor http_context, ILogger<WebAPIJsonWebTokenHandler> logger
    ) : JsonWebTokenHandler
{
    private static TokenValidationParameters GetValidationParameters(ApplicationOption app)
    {

        TokenValidationParameters parms = new();

        var security_key = CryptClass.GetSecurityKey(app.JWTKey, app.ApplicationName);
        parms.ValidIssuer = app.JWTIssuer;
        parms.ValidAudience = string.IsNullOrEmpty(app.JWTAudience) ? app.JWTIssuer : app.JWTAudience;
        parms.IssuerSigningKey = security_key;
        parms.TokenDecryptionKey = security_key;
        parms.ClockSkew = TimeSpan.Zero;

        return parms;
    }

    /// <summary>
    /// Generates an encrypted JWT whose claims are intended for backend use only.
    /// </summary>
    /// <param name="claims">Claims to embed in the token.</param>
    /// <param name="app">Application configuration providing signing and encryption settings.</param>
    /// <returns>A <see cref="TokenResult"/> containing the token and its descriptor.</returns>
    public TokenResult GenerateJwtTokenWEBApi(Dictionary<string, object> claims, ApplicationOption app)
    {
        var securityKey = CryptClass.GetSecurityKey(app.JWTKey, app.ApplicationName);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var encrypting_credentials = new EncryptingCredentials(securityKey, SecurityAlgorithms.Aes256KeyWrap, SecurityAlgorithms.Aes256CbcHmacSha512);

        // check claims for null or DBNull values and replace with empty string
        foreach (var claim in claims)
        {
            if (claim.Value == null || claim.Value == DBNull.Value)
            {
                claims[claim.Key] = "";
            }
        }

        var sd = new SecurityTokenDescriptor()
        {
            Issuer = app.JWTIssuer,
            Expires = DateTime.UtcNow.AddMinutes(app.JWTTokenExpirationMinutes),
            SigningCredentials = credentials,
            EncryptingCredentials = encrypting_credentials,
            Claims = claims,
            Audience = string.IsNullOrEmpty(app.JWTAudience) ? app.JWTIssuer : app.JWTAudience
        };

        string token = CreateToken(sd);

        return new() { Token = token, SD = sd };
    }

    /// <summary>
    /// Validates a JWT without enforcing the token's expiration time.
    /// </summary>
    /// <param name="app">Application configuration used to build validation parameters.</param>
    /// <param name="token">The JWT string to validate.</param>
    /// <returns>The claims principal extracted from the token when validation succeeds; otherwise, <c>null</c>.</returns>
    public async Task<ClaimsPrincipal?> ValidateExpiredToken(ApplicationOption app, string token)
    {
        var parms = GetValidationParameters(app);
        parms.ValidateAudience = false;
        parms.ValidateIssuer = false;
        parms.ValidateIssuerSigningKey = true;
        parms.ValidateLifetime = false;
        parms.ValidateActor = false;

        ClaimsPrincipal? principal = null;
        // MMC: When using SQL Server Authenticator, a new encryption key is generated every time the server is restarted
        // this validator uses in-memory data to keep configuration and is not persisted on purpose.
        // So we expect that when refreshing to get a SecurityTokenKeyWrapException
        try
        {
            // MMC: we have overrided the validate token to get dynamic parameters based on app_id, so we need to call base with the new parameters
            var validationResult = await base.ValidateTokenAsync(token, parms);

            if (validationResult.IsValid)
            {
                principal = new ClaimsPrincipal(validationResult.ClaimsIdentity);
            }
            else
            {
                logger.LogWarning("ValidateExpiredToken: Failed validation: {message}", validationResult.Exception.Message);
            }

            if (validationResult.SecurityToken is not JsonWebToken) throw new SecurityTokenException("Invalid token: not a JsonWebToken");

            return principal;

        }
        catch (Exception ex)
        {
            if (app.AuthenticationType == nameof(AuthenticationTypes.SQLServerAuthentication))
            {
                logger.LogWarning("ValidateExpiredToken: Invalid encryption key for app {app_id} with SQLServerAuthentication (this is expected with this type of auth). {message}", app.ApplicationID, ex.Message);
            }
            else
            {
                logger.LogError("ValidateExpiredToken error: {ex}", ex);
            }
        }

        return null;
    }


    private TokenValidationParameters GetValidationParametersFromContext()
    {
        string app_id = http_context.HttpContext?.Request?.RouteValues["app_id"]?.ToString() ?? "";
        TokenValidationParameters token_parms = new();
        if (!string.IsNullOrEmpty(app_id))
        {
            ApplicationOption? app = app_config.GetAppConfiguration(app_id);
            if (app != null) token_parms = GetValidationParameters(app);
        }

        return token_parms;
    }

    private void SetContextTokenValidationParameters(TokenValidationParameters target_parms)
    {
        TokenValidationParameters context_parms = GetValidationParametersFromContext();

        // MMC: merge with received validation parameters
        if (target_parms != null)
        {
            target_parms.ValidIssuer = context_parms.ValidIssuer;
            target_parms.ValidAudience = context_parms.ValidAudience;
            target_parms.IssuerSigningKey = context_parms.IssuerSigningKey;
            target_parms.TokenDecryptionKey = context_parms.TokenDecryptionKey;
            target_parms.ClockSkew = context_parms.ClockSkew;
        }

    }

    /// <summary>
    /// Validates a JWT token string using context-aware validation parameters.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <param name="validationParameters">Validation settings provided by the caller.</param>
    /// <returns>The result of token validation.</returns>
    public override Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
    {

        SetContextTokenValidationParameters(validationParameters);

        return base.ValidateTokenAsync(token, validationParameters);
    }

    /// <summary>
    /// Validates a JWT <see cref="SecurityToken"/> instance using context-aware parameters.
    /// </summary>
    /// <param name="token">Token instance to validate.</param>
    /// <param name="validationParameters">Validation settings provided by the caller.</param>
    /// <returns>The result of token validation.</returns>
    public override Task<TokenValidationResult> ValidateTokenAsync(SecurityToken token, TokenValidationParameters validationParameters)
    {
        SetContextTokenValidationParameters(validationParameters);

        return base.ValidateTokenAsync(token, validationParameters);
    }
}
