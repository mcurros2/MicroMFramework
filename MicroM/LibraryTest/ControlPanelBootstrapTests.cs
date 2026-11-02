using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MicroM.Configuration;
using MicroM.Configuration.Entities;
using MicroM.DataDictionary.Entities;
using MicroM.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LibraryTest;

[TestClass]
[DoNotParallelize]
public class ControlPanelBootstrapTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task FailedConfigurationLoad_KeepsControlPanelAndCorsAvailable(bool previouslyConfigured)
    {
        var restorations = new List<Action>();
        foreach (var field in typeof(MicroMAppConfigurationProvider).GetFields(BindingFlags.Static | BindingFlags.NonPublic))
        {
            if (field.GetValue(null) is IDictionary dictionary)
            {
                var saved = new List<DictionaryEntry>();
                foreach (DictionaryEntry entry in dictionary) saved.Add(entry);
                restorations.Add(() =>
                {
                    dictionary.Clear();
                    foreach (var entry in saved) dictionary.Add(entry.Key, entry.Value);
                });
                dictionary.Clear();
            }
        }
        var origins = (HashSet<string>)typeof(MicroMAppConfigurationProvider)
            .GetField("_globalAllowedURLS", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var savedOrigins = new List<string>(origins);
        origins.Clear();
        try
        {
            var apps = (Dictionary<string, ApplicationOption>)typeof(MicroMAppConfigurationProvider)
                .GetField("_ApplicationsCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            var types = (Dictionary<string, Type>)typeof(MicroMAppConfigurationProvider)
                .GetField("_EntityTypesCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            var assemblies = (Dictionary<string, List<Assembly>>)typeof(MicroMAppConfigurationProvider)
                .GetField("_EntityAssembliesCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            var existing = new ApplicationOption { ApplicationID = "existing", FrontendURLS = ["https://app.example"] };
            var existingAssemblies = new List<Assembly> { typeof(ConfigurationDB).Assembly };
            if (previouslyConfigured)
            {
                apps.Add(existing.ApplicationID, existing);
                types.Add("existing.ConfigurationDB", typeof(ConfigurationDB));
                assemblies.Add(existing.ApplicationID, existingAssemblies);
            }
            var runtime = new Mock<IAppAssemblyRuntimeManager>();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://control.example/"
            }).Build();
            var provider = new MicroMAppConfigurationProvider(
                Options.Create(new MicroMOptions
                {
                    SecretsFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                    SecretsFileName = "secrets", ConfigSQLServer = "", ConfigSQLServerDB = "missing",
                    EnableHotReloadForEntitiesAssemblies = true
                }),
                NullLogger<MicroMAppConfigurationProvider>.Instance, Mock.Of<IMicroMEncryption>(),
                Mock.Of<IBackgroundTaskQueue>(), Mock.Of<IMemoryEventsService>(), config,
                runtime.Object, Mock.Of<IAssemblyShadowCopyService>(), Mock.Of<IHostApplicationLifetime>());
            if (previouslyConfigured)
                typeof(MicroMAppConfigurationProvider).GetField("_activeAssemblyGeneration", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(provider, "active");

            // No server or secrets: fail before querying SQL, just as on an unconfigured API.
            for (int attempt = 0; attempt < 2; attempt++)
            {
                Assert.IsFalse(await provider.RefreshConfiguration(null, CancellationToken.None));
                Assert.IsNotNull(provider.GetAppConfiguration(ConfigurationDefaults.ControlPanelAppID));
                Assert.AreEqual(typeof(ConfigurationDB), provider.GetEntityType(ConfigurationDefaults.ControlPanelAppID, nameof(ConfigurationDB)));
                Assert.IsTrue(provider.IsDDType(nameof(MicromUsers)));
                CollectionAssert.Contains(provider.GetAllAPPAssemblies(ConfigurationDefaults.ControlPanelAppID), typeof(ConfigurationDB).Assembly);
                Assert.IsTrue(provider.IsCORSOriginAllowed(ConfigurationDefaults.ControlPanelAppID, "https://control.example"));
                Assert.IsFalse(provider.IsCORSOriginAllowed(ConfigurationDefaults.ControlPanelAppID, "https://unlisted.example"));
                Assert.AreEqual(previouslyConfigured, provider.IsCORSOriginAllowed("existing", "https://app.example"));
                if (previouslyConfigured)
                {
                    Assert.AreSame(existing, provider.GetAppConfiguration("existing"));
                    Assert.AreEqual(typeof(ConfigurationDB), provider.GetEntityType("existing", nameof(ConfigurationDB)));
                    Assert.AreSame(existingAssemblies, assemblies["existing"]);
                }
            }
            runtime.Verify(r => r.EnableFileWatchers("active", It.IsAny<Func<string, Task>>()),
                previouslyConfigured ? Times.Exactly(2) : Times.Never());
        }
        finally
        {
            foreach (var restore in restorations) restore();
            origins.Clear();
            origins.UnionWith(savedOrigins);
        }
    }
}
