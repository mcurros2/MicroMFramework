using System.Threading;
using System.Threading.Tasks;
using MicroM.Configuration;
using MicroM.Core;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LibraryTest;

[TestClass]
public class RecoveryOriginTests
{
    [TestMethod]
    [DataRow("https://customers.myapp.com", "https://customers.myapp.com")]
    [DataRow("https://BACKOFFICE.myapp.com:443", "https://backoffice.myapp.com")]
    [DataRow("https://customers.myapp.com/", "https://customers.myapp.com")]
    [DataRow("https://customers.myapp.com:8443", "https://customers.myapp.com:8443")]
    [DataRow("http://localhost:3000", "http://localhost:3000")]
    [DataRow(null, null)]
    [DataRow("", null)]
    [DataRow("null", null)]
    [DataRow("https://other-app.myapp.com", null)]
    [DataRow("https://customers.myapp.com.evil.com", null)]
    [DataRow("https://customers.myapp.com@evil.com", null)]
    [DataRow("https://user@customers.myapp.com", null)]
    [DataRow("https://customers.myapp.com/recover", null)]
    [DataRow("https://customers.myapp.com?x=1", null)]
    [DataRow("https://customers.myapp.com#fragment", null)]
    [DataRow("https://customers.myapp.com, https://backoffice.myapp.com", null)]
    [DataRow("http://customers.myapp.com", null)]
    [DataRow("https://customers.myapp.com:444", null)]
    [DataRow("https://customers.myapp.com/a/..", null)]
    [DataRow("https://customers.myapp.com\n", null)]
    [DataRow("https://customers.myapp.com\\recover", null)]
    [DataRow("//customers.myapp.com", null)]
    public async Task RecoveryEmail_UsesOnlyConfiguredAppOrigin(string? origin, string? expected)
    {
        var app = new ApplicationOption
        {
            ApplicationID = "test",
            FrontendURLS = ["https://customers.myapp.com/", "https://backoffice.myapp.com",
                "https://customers.myapp.com:8443", "http://localhost:3000", "http://customers.myapp.com"]
        };
        var config = new Mock<IMicroMAppConfiguration>();
        config.Setup(c => c.GetAppConfiguration("test")).Returns(app);
        // Even globally allowed origins must not authorize recovery destinations.
        config.Setup(c => c.IsCORSOriginAllowed(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        var authenticator = new Mock<IAuthenticator>(MockBehavior.Strict);
        var provider = new Mock<IAuthenticationProvider>(MockBehavior.Strict);
        if (expected != null)
        {
            provider.Setup(p => p.GetAuthenticator(app)).Returns(authenticator.Object);
            authenticator.Setup(a => a.SendPasswordRecoveryEmail(app, "user", expected, CancellationToken.None))
                .ReturnsAsync(new ResultWithStatus<bool, string>(false, null));
        }
        var service = new AuthenticationService(NullLogger<AuthenticationService>.Instance,
            config.Object, Mock.Of<IDeviceIdService>(), Mock.Of<IMicroMEncryption>());

        var result = await service.HandleSendRecoveryEmail(provider.Object, "test", "user", origin, CancellationToken.None);

        Assert.AreEqual(expected == null, result.Result);
        provider.VerifyAll();
        authenticator.VerifyAll();
        if (expected == null) provider.Verify(p => p.GetAuthenticator(It.IsAny<ApplicationOption>()), Times.Never);
        config.Verify(c => c.IsCORSOriginAllowed(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
