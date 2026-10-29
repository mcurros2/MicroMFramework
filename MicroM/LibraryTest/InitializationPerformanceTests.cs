using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Database;
using MicroM.DataDictionary.Entities;
using MicroM.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LibraryTest;

[TestClass]
public class InitializationPerformanceTests
{
    [TestMethod]
    public async Task SharedAssembly_RejectsConflictingRelativeTestData()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var first = Path.Combine(folder, "first");
            var second = Path.Combine(folder, "second");
            var options = new MicroMOptions { EntitiesDLLStoragePath = Path.Combine(folder, "output") };
            foreach (var source in new[] { first, second })
            {
                Directory.CreateDirectory(Path.Combine(source, options.TestDataRootFolderName));
                File.Copy(typeof(ApplicationDatabase).Assembly.Location, Path.Combine(source, "shared.dll"));
                File.WriteAllText(Path.Combine(source, options.TestDataRootFolderName, "data.json"), source == first ? "[]" : "[{}]");
            }
            var service = new AssemblyShadowCopyService(Options.Create(options), NullLogger<AssemblyShadowCopyService>.Instance);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CopyForReloadAsync([
                new AssemblyCopyRequest("A", Path.Combine(first, "shared.dll"), "one"),
                new AssemblyCopyRequest("B", Path.Combine(second, "shared.dll"), "two")], CancellationToken.None));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [TestMethod]
    public void ExplicitUpdate_SelectsApplicationOnly_NotOtherConsumersOfItsAssembly()
    {
        var apps = new[] {
            new ApplicationOption { ApplicationID = "A", EnableUpdateOnHotReload = true },
            new ApplicationOption { ApplicationID = "B", EnableUpdateOnHotReload = true }
        };
        var select = typeof(MicroMAppConfigurationProvider).GetMethod("SelectDatabaseUpdateTargets", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var targets = ((IEnumerable<ApplicationOption>)select.Invoke(null, new object[] { apps, "A" })!).ToArray();
        CollectionAssert.AreEqual(new[] { "A" }, targets.Select(a => a.ApplicationID).ToArray());
    }

    [TestMethod]
    public async Task MetadataSynchronization_DoesNotWriteUnchangedRows_AndKeepsDisconnect()
    {
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(System.Data.ConnectionState.Open);
        var entity = new MicromEntitiesTypes(client.Object);
        var entities = new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>();
        entities.Add(nameof(MicromEntitiesTypes), new(entity));
        var rows = new DataResult(2);
        rows.records.Add([nameof(MicromEntitiesTypes), typeof(MicromEntitiesTypes).FullName]);
        client.Setup(c => c.ExecuteSP("[dbo].mty_GetEntitiesTypes", It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<DataResult> { rows });
        await MicromEntitiesTypes.FillEntitiesTypes(client.Object, "dbo", entities, CancellationToken.None);
        client.Verify(c => c.ExecuteSPNonQuery(It.IsAny<string>(), It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<CancellationToken>()), Times.Never);
        client.Verify(c => c.ExecuteSP("[dbo].mty_GetEntitiesTypes", It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.ExecuteSP<DBStatus>(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<AutoMapperMode>(), It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<IEntityClient.MapResult<DBStatus>>()), Times.Never);
        client.Verify(c => c.ExecuteSQL(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        client.Verify(c => c.BeginTransaction(It.IsAny<CancellationToken>()), Times.Never);
        client.Verify(c => c.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);
        client.Verify(c => c.RollbackTransaction(It.IsAny<CancellationToken>()), Times.Never);
        client.Verify(c => c.Disconnect(), Times.Once);
    }

    [TestMethod]
    public async Task SharedAssembly_IsCopiedOnce_WithBothApplicationMappings()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var service = new AssemblyShadowCopyService(Options.Create(new MicroMOptions { EntitiesDLLStoragePath = folder }),
                NullLogger<AssemblyShadowCopyService>.Instance);
            var source = typeof(ApplicationDatabase).Assembly.Location;
            var generation = await service.CopyForReloadAsync([
                new AssemblyCopyRequest("A", source, "one"), new AssemblyCopyRequest("B", source, "two")], CancellationToken.None);
            Assert.HasCount(2, generation.files);
            Assert.AreEqual(generation.files[0].copied_assembly_path, generation.files[1].copied_assembly_path);
            Assert.HasCount(1, Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories));
            CollectionAssert.AreEquivalent(new[] { "A", "B" }, generation.files.Select(f => f.app_id).ToArray());
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [TestMethod]
    public async Task SharedAssembly_IsLoadedOnce_WithBothApplicationMappings()
    {
        var source = typeof(ApplicationDatabase).Assembly.Location;
        await using var service = new AssemblyLoadContextService(NullLogger<AssemblyLoadContextService>.Instance);
        var generation = await service.LoadGenerationAsync(new AssemblyShadowCopyGeneration("shared", [
            new AssemblyCopiedFile("A", source, source), new AssemblyCopiedFile("B", source, source)], []), CancellationToken.None);
        Assert.AreSame(generation.AssembliesByKey[$"A|{source}"], generation.AssembliesByKey[$"B|{source}"]);
    }

    [TestMethod]
    public async Task TableDiscovery_UsesOneQuery_AndPreservesCallerConnection()
    {
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(System.Data.ConnectionState.Open);
        var entity = new MicromEntitiesTypes(client.Object, schema_name: "custom");
        var entities = new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>();
        entities.Add(nameof(MicromEntitiesTypes), new(entity));
        var rows = new DataResult(2);
        rows.records.Add(["custom", entity.Def.TableName]);
        client.Setup(c => c.ExecuteSQL(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<DataResult> { rows });
        var missing = await DatabaseSchemaTables.GetEntitiesInexistingTables(client.Object, entities, CancellationToken.None);
        Assert.HasCount(0, missing);
        client.Verify(c => c.ExecuteSQL(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.Disconnect(), Times.Never);
        client.Verify(c => c.Clone(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }
}
