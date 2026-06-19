using MicroM.Configuration;
using MicroM.Web.Authentication.SSO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;

namespace LibraryTest;

[TestClass]
public class PushedAuthorizationServiceTests
{
    private PushedAuthorizationService _service = new();

    private ApplicationOption MakeAppWithClient(string clientId, string redirect)
    {
        var app = new ApplicationOption
        {
            ApplicationID = "app1",
            OIDCClientConfiguration = new Dictionary<string, OIDCClientConfigurationOption>
            {
                [clientId] = new OIDCClientConfigurationOption
                {
                    ApplicationID = clientId,
                    ClientAPPID = clientId,
                    URLAuthorizedRedirects = new List<string> { redirect }
                }
            }
        };
        return app;
    }

    // Provide sensible defaults for PKCE so tests that don't explicitly pass PKCE still satisfy validation.
    private IFormCollection MakeForm(string response_type, string redirect_uri, string scope, string state = "", string code_challenge = "challenge", string code_challenge_method = "S256")
    {
        var dict = new Dictionary<string, StringValues>
        {
            ["client_id"] = "client1",
            ["response_type"] = response_type,
            ["redirect_uri"] = redirect_uri,
            ["scope"] = scope,
            ["state"] = state,
            ["code_challenge"] = code_challenge,
            ["code_challenge_method"] = code_challenge_method
        };
        return new FormCollection(dict);
    }

    [TestMethod]
    public void CreatePushedRequest_AllowsExactMatch()
    {
        var clientId = "client1";
        var redirect = "https://app.example.com/callback";
        var app = MakeAppWithClient(clientId, redirect);

        var form = MakeForm("code", redirect, "openid");

        var (response, error) = _service.CreatePushedRequest(app, form, clientId);

        Assert.IsNull(error, "expected no error");
        Assert.IsNotNull(response, "expected response");
        Assert.IsTrue(response!.request_uri.StartsWith("urn:ietf:params:oauth:request_uri:"), "unexpected request_uri");
        Assert.AreEqual(90, response.expires_in);
    }

    [TestMethod]
    public void CreatePushedRequest_AllowsTrailingSlashNormalization()
    {
        var clientId = "client1";
        var redirectRegistered = "https://app.example.com/callback";
        var redirectIncoming = "https://app.example.com/callback/";
        var app = MakeAppWithClient(clientId, redirectRegistered);

        var form = MakeForm("code", redirectIncoming, "openid");

        var (response, error) = _service.CreatePushedRequest(app, form, clientId);

        Assert.IsNull(error, "expected no error");
        Assert.IsNotNull(response, "expected response");
    }

    [TestMethod]
    public void CreatePushedRequest_RejectsDifferentHost()
    {
        var clientId = "client1";
        var redirectRegistered = "https://app.example.com/callback";
        var redirectIncoming = "https://evil.example.com/callback";
        var app = MakeAppWithClient(clientId, redirectRegistered);

        var form = MakeForm("code", redirectIncoming, "openid");

        var (response, error) = _service.CreatePushedRequest(app, form, clientId);

        Assert.IsNull(response);
        Assert.IsNotNull(error);
    }

    [TestMethod]
    public void CreatePushedRequest_RejectsDifferentQuery()
    {
        var clientId = "client1";
        var redirectRegistered = "https://app.example.com/callback?foo=1";
        var redirectIncoming = "https://app.example.com/callback?foo=2";
        var app = MakeAppWithClient(clientId, redirectRegistered);

        var form = MakeForm("code", redirectIncoming, "openid");

        var (response, error) = _service.CreatePushedRequest(app, form, clientId);

        Assert.IsNull(response);
        Assert.IsNotNull(error);
    }

    [TestMethod]
    public void ConsumeRequest_OneTimeConsumption()
    {
        var clientId = "client1";
        var redirect = "https://app.example.com/callback";
        var app = MakeAppWithClient(clientId, redirect);

        var form = MakeForm("code", redirect, "openid");
        var (response, error) = _service.CreatePushedRequest(app, form, clientId);

        if (error != null) Debug.WriteLine(error);
        Assert.IsNotNull(response);
        Assert.IsNull(error);

        if (response.request_uri != null) Debug.WriteLine(response.request_uri);
        Assert.IsNotNull(response.request_uri);

        var requestUri = response!.request_uri;
        var req = _service.ConsumeRequest(requestUri);
        Assert.IsNotNull(req);
        Assert.AreEqual(clientId, req.request?.client_id);

        var req2 = _service.ConsumeRequest(requestUri);
        Debug.WriteLine(req2);
        Assert.IsNull(req2.request);
        Assert.IsNull(req2.header);
        Assert.IsNull(req2.rawObject);
    }
}