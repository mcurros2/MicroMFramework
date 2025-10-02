using Microsoft.AspNetCore.Http;

namespace MicroM.Web.Authentication.SSO;

public sealed record StateAndNonceRecord(
    string State,
    string Nonce,
    string? DeviceId,
    IFormCollection? AdjustedForm
);
