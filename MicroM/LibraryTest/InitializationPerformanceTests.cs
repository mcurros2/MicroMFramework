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
    public async Task SharedAssembly_CopiesEachApplicationsOwnNestedTestData()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var first = Path.Combine(folder, "first");
            var second = Path.Combine(folder, "second");
            var options = new MicroMOptions { EntitiesDLLStoragePath = Path.Combine(folder, "output"), TestDataRootFolderName = "custom-data" };
            foreach (var source in new[] { first, second })
            {
                Directory.CreateDirectory(Path.Combine(source, options.TestDataRootFolderName, "nested"));
                File.Copy(typeof(ApplicationDatabase).Assembly.Location, Path.Combine(source, "shared.dll"));
                File.WriteAllText(Path.Combine(source, options.TestDataRootFolderName, "nested", "data.json"), source == first ? "[]" : "[{}]");
            }
            var service = new AssemblyShadowCopyService(Options.Create(options), NullLogger<AssemblyShadowCopyService>.Instance);
            var generation = await service.CopyForReloadAsync([
                new AssemblyCopyRequest("A", Path.Combine(first, "shared.dll"), "one"),
                new AssemblyCopyRequest("B", Path.Combine(second, "shared.dll"), "two")], CancellationToken.None);
            foreach (var file in generation.files)
            {
                var data = Path.Combine(Path.GetDirectoryName(file.copied_assembly_path)!, options.TestDataRootFolderName, "nested", "data.json");
                Assert.AreEqual(file.app_id == "A" ? "[]" : "[{}]", File.ReadAllText(data));
            }
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
    public async Task SharedAssembly_IsCopiedForEachApplication_WithBothMappings()
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
            Assert.AreNotEqual(generation.files[0].copied_assembly_path, generation.files[1].copied_assembly_path);
            Assert.HasCount(2, Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories));
            CollectionAssert.AreEquivalent(new[] { "A", "B" }, generation.files.Select(f => f.app_id).ToArray());
            foreach (var file in generation.files)
            {
                Assert.AreEqual(Path.Combine(folder, file.app_id, generation.generation_id, Path.GetFileName(source)), file.copied_assembly_path);
                CollectionAssert.AreEqual(File.ReadAllBytes(source), File.ReadAllBytes(file.copied_assembly_path));
            }
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [TestMethod]
    public async Task ShadowCopy_NewGenerationIncludesCurrentFiles_AndStartupCleanupRemovesOldCopies()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var source = Path.Combine(folder, "source");
            var output = Path.Combine(folder, "output");
            Directory.CreateDirectory(Path.Combine(source, "data", "nested"));
            var dll = Path.Combine(source, "entities.dll");
            var data = Path.Combine(source, "data", "nested", "test.json");
            File.WriteAllText(dll, "first DLL contents");
            File.WriteAllText(data, "[]");
            var service = new AssemblyShadowCopyService(Options.Create(new MicroMOptions { EntitiesDLLStoragePath = output, TestDataRootFolderName = "data" }),
                NullLogger<AssemblyShadowCopyService>.Instance);
            var requests = new[] { new AssemblyCopyRequest("A", dll, "one") };
            var first = await service.CopyForReloadAsync(requests, CancellationToken.None);

            File.WriteAllText(dll, "new DLL contents");
            File.WriteAllText(data, "[{}]");
            var second = await service.CopyForReloadAsync(requests, CancellationToken.None);
            Assert.AreNotEqual(first.generation_id, second.generation_id);
            Assert.AreEqual("first DLL contents", File.ReadAllText(first.files.Single().copied_assembly_path));
            Assert.AreEqual("new DLL contents", File.ReadAllText(second.files.Single().copied_assembly_path));
            Assert.AreEqual("[{}]", File.ReadAllText(Path.Combine(output, "A", second.generation_id, "data", "nested", "test.json")));

            await service.DeleteAllGenerationsExceptAsync(second.generation_id, CancellationToken.None);
            Assert.IsFalse(Directory.Exists(Path.Combine(output, "A", first.generation_id)));
            Assert.IsTrue(File.Exists(second.files.Single().copied_assembly_path));
            await service.DeleteAllGenerationsAsync(CancellationToken.None);
            Assert.HasCount(0, Directory.GetDirectories(output));
            var third = await service.CopyForReloadAsync(requests, CancellationToken.None);
            Assert.AreNotEqual(second.generation_id, third.generation_id);
            Assert.AreEqual("new DLL contents", File.ReadAllText(third.files.Single().copied_assembly_path));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [TestMethod]
    public async Task ShadowCopy_PropagatesMissingFileAndCancellation()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(folder);
            var source = Path.Combine(folder, "missing.dll");
            var service = new AssemblyShadowCopyService(Options.Create(new MicroMOptions { EntitiesDLLStoragePath = Path.Combine(folder, "output") }),
                NullLogger<AssemblyShadowCopyService>.Instance);
            var requests = new[] { new AssemblyCopyRequest("A", source, "one") };
            await Assert.ThrowsExactlyAsync<FileNotFoundException>(() => service.CopyForReloadAsync(requests, CancellationToken.None));
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => service.CopyForReloadAsync(requests, new CancellationToken(true)));
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
    public async Task FailedGenerationPreparation_KeepsActiveGenerationAndItsWatchers()
    {
        var source = typeof(ApplicationDatabase).Assembly.Location;
        var files = new[] { new AssemblyCopiedFile("A", source, source) };
        var active = new AssemblyShadowCopyGeneration("active", files, []);
        var failed = new AssemblyShadowCopyGeneration("failed", files, []);
        var shadow = new Mock<IAssemblyShadowCopyService>();
        shadow.SetupSequence(s => s.CopyForReloadAsync(It.IsAny<IReadOnlyCollection<AssemblyCopyRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(active).ReturnsAsync(failed);
        var contexts = new Mock<IAssemblyLoadContextService>();
        contexts.Setup(c => c.LoadGenerationAsync(active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssemblyLoadContextGeneration("active", new Dictionary<string, System.Reflection.Assembly>()));
        contexts.Setup(c => c.LoadGenerationAsync(failed, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileLoadException("Invalid assembly"));
        var watchers = new Mock<IAssemblyFileWatcherService>();
        await using var runtime = new AppAssemblyRuntimeManager(shadow.Object, contexts.Object, watchers.Object,
            NullLogger<AppAssemblyRuntimeManager>.Instance);
        var requests = new[] { new AssemblyCopyRequest("A", source, "one") };
        var prepared = await runtime.PrepareGenerationAsync(requests, CancellationToken.None);
        await runtime.CommitGenerationAsync(prepared.generation_id, CancellationToken.None);

        await Assert.ThrowsExactlyAsync<FileLoadException>(() => runtime.PrepareGenerationAsync(requests, CancellationToken.None));
        runtime.EnableFileWatchers("active", _ => Task.CompletedTask);
        watchers.Verify(w => w.ResetWatchers(It.Is<IEnumerable<string>>(paths => paths.SequenceEqual(new[] { source })), It.IsAny<Func<string, Task>>()), Times.Once);
        contexts.Verify(c => c.UnloadGenerationAsync("active", It.IsAny<CancellationToken>()), Times.Never);
        shadow.Verify(s => s.DeleteGenerationAsync("active", It.IsAny<CancellationToken>()), Times.Never);
        shadow.Verify(s => s.DeleteGenerationAsync("failed", It.IsAny<CancellationToken>()), Times.Once);
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
