namespace MicroM.Web.Authentication;

public sealed record OIDCHttpClientPostResponse(
    int StatusCode,
    bool IsSuccessStatusCode,
    string ContentType,
    string Body
);


