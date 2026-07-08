using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
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
    public void TotpSetupStartResponse_SerializesOnlyQrCodeDataUrl()
    {
        TotpSetupStartResponse response = new()
        {
            qr_code_data_url = "data:image/png;base64,test"
        };

        string json = JsonSerializer.Serialize(response);

        Assert.IsTrue(json.Contains("qr_code_data_url", StringComparison.Ordinal));
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

        TotpServiceResult result = await service.HandleStartTotpSetup(auth.Object, "missing", "user", [], CancellationToken.None);

        Assert.AreEqual(TotpServiceResultStatus.AppNotFound, result.Status);
    }
}
