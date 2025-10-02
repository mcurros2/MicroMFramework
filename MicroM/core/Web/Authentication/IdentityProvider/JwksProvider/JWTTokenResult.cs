using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace MicroM.Web.Authentication.SSO;

public record JWTTokenResult
(
    ClaimsPrincipal Principal,
    JsonWebToken Token,
    DateTimeOffset? ExpiresUtc
);

