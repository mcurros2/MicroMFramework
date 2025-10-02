namespace MicroM.Web.Authentication;

public sealed record OIDCHttpClientPostResponse(
    int StatusCode,
    string ContentType,
    string Body
);


