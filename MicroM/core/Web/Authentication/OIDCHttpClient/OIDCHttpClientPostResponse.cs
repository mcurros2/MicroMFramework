namespace MicroM.Web.Authentication;

public sealed record OIDCHttpClientPostResponse(
    int StatusCode,
    bool IsSuccessStatusCode = false,
    string ContentType = "",
    string Body = "",
    string? Error = null,
    string? ETag = null,
    bool NotModified = false
);


