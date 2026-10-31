using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MicroM.Configuration;
using MicroM.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LibraryTest;

[TestClass]
[DoNotParallelize]
public class ConfigurationRefreshCredentialsTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RefreshFailure_RetainsFreshCredentials_AndReadsSecretsOnce(bool initiallyConfigured)
    {
        var apps = GetApps();
        var saved = new Dictionary<string, ApplicationOption>(apps);
        var services = (Dictionary<string, List<IMicroMApplicationServices>>)typeof(MicroMAppConfigurationProvider)
            .GetField("_ApplicationServicesCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var savedServices = new Dictionary<string, List<IMicroMApplicationServices>>(services);
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            apps.Clear();
            services.Clear();
            if (initiallyConfigured) apps[ConfigurationDefaults.ControlPanelAppID] = new ApplicationOption
            {
                ApplicationID = ConfigurationDefaults.ControlPanelAppID, SQLServer = "", SQLUser = "", SQLPassword = "old-password"
            };
            Directory.CreateDirectory(Path.Combine(folder, "MicroM"));
            File.WriteAllText(Path.Combine(folder, "MicroM", "secrets"), "encrypted-test-data");
            var encryption = new Mock<IMicroMEncryption>();
            encryption.Setup(e => e.DecryptObject<SecretsOptions>("encrypted-test-data"))
                .Returns(new SecretsOptions { ConfigSQLUser = "", ConfigSQLPassword = "new-password" });
            var runtime = new Mock<IAppAssemblyRuntimeManager>();
            var provider = CreateProvider(folder, encryption.Object, runtime.Object);
            if (initiallyConfigured)
                typeof(MicroMAppConfigurationProvider).GetField("_activeAssemblyGeneration", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(provider, "active");

            // Empty server/user deliberately exercises restoration without contacting SQL Server.
            Assert.IsFalse(await provider.RefreshConfiguration(null, CancellationToken.None));
            Assert.AreEqual("new-password", provider.GetAppConfiguration(ConfigurationDefaults.ControlPanelAppID)!.SQLPassword);
            Assert.IsFalse(await provider.RefreshConfiguration(null, CancellationToken.None));
            Assert.AreEqual("new-password", provider.GetAppConfiguration(ConfigurationDefaults.ControlPanelAppID)!.SQLPassword);
            encryption.Verify(e => e.DecryptObject<SecretsOptions>("encrypted-test-data"), Times.Exactly(2));
            runtime.Verify(r => r.DisableFileWatchers(), Times.Exactly(2));
            runtime.Verify(r => r.EnableFileWatchers("active", It.IsAny<Func<string, Task>>()), initiallyConfigured ? Times.Exactly(2) : Times.Never());
        }
        finally
        {
            apps.Clear();
            foreach (var item in saved) apps[item.Key] = item.Value;
            services.Clear();
            foreach (var item in savedServices) services[item.Key] = item.Value;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    [TestMethod]
    public async Task ActiveGeneration_ReadsSecretsBeforeAttemptingConfigurationQueries()
    {
        var apps = GetApps();
        var saved = new Dictionary<string, ApplicationOption>(apps);
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            apps.Clear();
            apps[ConfigurationDefaults.ControlPanelAppID] = new ApplicationOption
            {
                ApplicationID = ConfigurationDefaults.ControlPanelAppID, SQLServer = "", SQLUser = "", SQLPassword = "old-password"
            };
            Directory.CreateDirectory(Path.Combine(folder, "MicroM"));
            File.WriteAllText(Path.Combine(folder, "MicroM", "secrets"), "encrypted-test-data");
            var encryption = new Mock<IMicroMEncryption>();
            encryption.Setup(e => e.DecryptObject<SecretsOptions>("encrypted-test-data"))
                .Throws(new FormatException("secrets-read-marker"));
            var provider = CreateProvider(folder, encryption.Object);
            typeof(MicroMAppConfigurationProvider).GetField("_activeAssemblyGeneration", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(provider, "active");

            var error = await Assert.ThrowsExactlyAsync<FormatException>(() => provider.RefreshConfiguration(null, CancellationToken.None));
            Assert.AreEqual("secrets-read-marker", error.Message);
            encryption.Verify(e => e.DecryptObject<SecretsOptions>("encrypted-test-data"), Times.Once);
        }
        finally
        {
            apps.Clear();
            foreach (var item in saved) apps[item.Key] = item.Value;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    private static Dictionary<string, ApplicationOption> GetApps() =>
        (Dictionary<string, ApplicationOption>)typeof(MicroMAppConfigurationProvider)
            .GetField("_ApplicationsCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

    private static MicroMAppConfigurationProvider CreateProvider(string folder, IMicroMEncryption encryption, IAppAssemblyRuntimeManager? runtime = null) => new(
        Options.Create(new MicroMOptions { SecretsFilePath = folder, SecretsFileName = "secrets", ConfigSQLServer = "", EnableHotReloadForEntitiesAssemblies = true }),
        NullLogger<MicroMAppConfigurationProvider>.Instance, encryption, Mock.Of<IBackgroundTaskQueue>(),
        Mock.Of<IMemoryEventsService>(), new ConfigurationBuilder().Build(), runtime ?? Mock.Of<IAppAssemblyRuntimeManager>(), Mock.Of<IAssemblyShadowCopyService>(), Mock.Of<IHostApplicationLifetime>());
}
