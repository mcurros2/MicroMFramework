using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static MicroM.Core.Base32;

namespace LibraryTest;

[TestClass]
public class TotpServiceTests
{
    private const string TestSecret = "JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP";

    [TestMethod]
    public void GetAuthenticatorQrCodeDataUrl_ReturnsPngDataUrl()
    {
        TotpService service = new();
        string uri = service.GetAuthenticatorUri("user@example.com", TestSecret, "MicroM");

        string dataUrl = service.GetAuthenticatorQrCodeDataUrl(uri);

        Assert.IsTrue(dataUrl.StartsWith("data:image/png;base64,", StringComparison.Ordinal));

        string base64 = dataUrl["data:image/png;base64,".Length..];
        byte[] pngBytes = Convert.FromBase64String(base64);
        byte[] pngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        CollectionAssert.AreEqual(pngHeader, pngBytes[..pngHeader.Length]);
    }

    [TestMethod]
    public void ComputeTotp_AllowsEmptyModifier()
    {
        byte[] key = Base32Decode(TestSecret);
        ulong timestep = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);

        string code = TotpService.ComputeTotp(key, timestep, ReadOnlySpan<byte>.Empty);

        Assert.AreEqual(6, code.Length);
        foreach (char c in code)
        {
            Assert.IsTrue(char.IsDigit(c));
        }
    }

    [TestMethod]
    public void VerifyCode_UsesEmptyModifierByDefault()
    {
        TotpService service = new();
        byte[] key = Base32Decode(TestSecret);
        ulong timestep = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        string code = TotpService.ComputeTotp(key, timestep, ReadOnlySpan<byte>.Empty);

        Assert.IsTrue(service.VerifyCode(TestSecret, code));
        Assert.IsTrue(service.VerifyCode(TestSecret, code, null));
        Assert.IsTrue(service.VerifyCode(TestSecret, code, ""));
    }

    [TestMethod]
    public void VerifyCode_UsesCustomModifier()
    {
        TotpService service = new();
        byte[] key = Base32Decode(TestSecret);
        byte[] modifier = Encoding.UTF8.GetBytes("CustomAuthenticator");
        ulong timestep = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        string code = TotpService.ComputeTotp(key, timestep, modifier);

        bool verified = service.VerifyCode(TestSecret, code, "CustomAuthenticator");

        Assert.IsTrue(verified);
        Assert.IsFalse(service.VerifyCode(TestSecret, code));
    }

    [TestMethod]
    public void TotpSetupStartResponse_SerializesOnlyPublicSetupFields()
    {
        TotpSetupStartResponse response = new()
        {
            setup_challenge_id = "challenge",
            qr_code_data_url = "data:image/png;base64,test"
        };

        string json = JsonSerializer.Serialize(response);

        Assert.IsTrue(json.Contains("qr_code_data_url", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("setup_challenge_id", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("secret", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("manual_entry_key", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("authenticator_uri", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task HandleStartTotpSetup_ReturnsAppNotFound()
    {
        Mock<IMicroMAppConfiguration> appConfig = new();
        appConfig.Setup(x => x.GetAppConfiguration("missing")).Returns((MicroM.Configuration.ApplicationOption?)null);

        TotpService service = new(app_config: appConfig.Object);
        Mock<IAuthenticationProvider> auth = new();

        TotpServiceResult result = await service.HandleStartTotpSetup(auth.Object, "missing", "user", new TotpSetupRequest { AuthenticatorName = "Phone" }, [], CancellationToken.None);

        Assert.AreEqual(TotpServiceResultStatus.AppNotFound, result.Status);
    }

    [TestMethod]
    public async Task HandleLoginTotpRegistration_RejectsRegisteredSqlAdminChallenge()
    {
        const string appId = "control-panel";
        MicroM.Configuration.ApplicationOption app = new()
        {
            ApplicationID = appId,
            AuthenticationType = nameof(MicroM.Configuration.CategoriesDefinitions.AuthenticationTypes.SQLServerAuthentication),
            SQLAdminTotpSecret = TestSecret
        };
        TwoFactorChallenge challenge = CreateSqlAdminChallenge(appId, TwoFactorFlows.SqlAdminAuthenticator, null);
        var (service, auth) = CreateSqlAdminRegistrationService(app, challenge);

        TotpServiceResult result = await service.HandleLoginTotpRegistration(auth.Object, appId, new TwoFactorRegistrationRequest { ChallengeId = "challenge" }, CancellationToken.None);

        Assert.AreEqual(TotpServiceResultStatus.SetupNotStarted, result.Status);
    }

    [TestMethod]
    public async Task HandleLoginTotpRegistration_UsesPendingSetupSecret()
    {
        const string appId = "control-panel";
        MicroM.Configuration.ApplicationOption app = new()
        {
            ApplicationID = appId,
            AuthenticationType = nameof(MicroM.Configuration.CategoriesDefinitions.AuthenticationTypes.SQLServerAuthentication)
        };
        TwoFactorChallenge challenge = CreateSqlAdminChallenge(appId, TwoFactorFlows.SqlAdminSetup, TestSecret);
        var (service, auth) = CreateSqlAdminRegistrationService(app, challenge);

        TotpServiceResult result = await service.HandleLoginTotpRegistration(auth.Object, appId, new TwoFactorRegistrationRequest { ChallengeId = "challenge" }, CancellationToken.None);

        Assert.AreEqual(TotpServiceResultStatus.Success, result.Status);
        Assert.IsNotNull(result.SetupResponse);
        Assert.AreEqual("challenge", result.SetupResponse.setup_challenge_id);
        Assert.IsTrue(result.SetupResponse.qr_code_data_url.StartsWith("data:image/png;base64,", StringComparison.Ordinal));
    }

    private static TwoFactorChallenge CreateSqlAdminChallenge(string appId, string flow, string? setupSecret)
    {
        Dictionary<string, string> metadata = new(StringComparer.OrdinalIgnoreCase)
        {
            [TwoFactorChallengeMetadataKeys.Flow] = flow
        };
        if (setupSecret != null) metadata[TwoFactorChallengeMetadataKeys.SetupTotpSecret] = setupSecret;

        return new()
        {
            UserId = "sa",
            Username = "sa",
            DeviceId = "none",
            ApplicationId = appId,
            LocalDeviceId = "",
            CreatedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.AddMinutes(5),
            Metadata = metadata
        };
    }

    private static (TotpService Service, Mock<IAuthenticationProvider> Auth) CreateSqlAdminRegistrationService(
        MicroM.Configuration.ApplicationOption app,
        TwoFactorChallenge challenge)
    {
        Mock<IMicroMAppConfiguration> appConfig = new();
        appConfig.Setup(x => x.GetAppConfiguration(app.ApplicationID)).Returns(app);

        Mock<ITwoFactorChallengeStore> challengeStore = new();
        challengeStore.Setup(x => x.GetChallenge("challenge")).Returns(challenge);

        Mock<IMicroMEncryption> encryptor = new();
        SQLServerAuthenticator sqlAuthenticator = new(
            NullLogger<SQLServerAuthenticator>.Instance,
            encryptor.Object,
            new HttpContextAccessor(),
            Options.Create(new MicroM.Configuration.MicroMOptions()),
            challengeStore.Object,
            new TotpService(),
            appConfig.Object);

        Mock<IAuthenticationProvider> auth = new();
        auth.Setup(x => x.GetAuthenticator(app)).Returns(sqlAuthenticator);

        return (new TotpService(app_config: appConfig.Object, challengeStore: challengeStore.Object), auth);
    }
}
