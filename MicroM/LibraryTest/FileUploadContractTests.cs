using MicroM.DataDictionary.Entities;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryTest;

[TestClass]
public class FileUploadContractTests
{
    [TestMethod]
    public void HttpFileContracts_DoNotExposeInternalDatabaseIds()
    {
        AssertContractHasNoInternalId(typeof(UploadFileResult));

        Assert.IsNotNull(typeof(UploadFileResult).GetProperty(nameof(UploadFileResult.vc_fileguid)));
    }

    [TestMethod]
    public void FileStoreClient_IsTheGuidOnlyUploaderEntity()
    {
        var definition = new FileStoreClientDef();

        Assert.AreEqual("fcc", definition.Mneo);
        Assert.IsTrue(definition.Fake);
        Assert.IsTrue(definition.Columns.Contains(nameof(FileStoreClientDef.vc_fileguid)));
        Assert.IsFalse(definition.Columns.Contains(nameof(FileStoreDef.c_file_id)));

        var expectedColumns = new FileStoreDef().Columns.Keys
            .Where(name => !name.Equals(nameof(FileStoreDef.c_file_id), StringComparison.OrdinalIgnoreCase))
            .ToArray();
        CollectionAssert.AreEquivalent(expectedColumns, definition.Columns.Keys.ToArray());

        Assert.IsTrue(EveryoneAllowedRoutes.IsEveryoneAllowedRoute("microm", "app", "/microm/app/ent/FileStoreClient/delete"));
        Assert.IsTrue(EveryoneAllowedRoutes.IsEveryoneAllowedRoute("microm", "app", "/microm/app/ent/FileStoreClient/view/fcc_brwFiles"));
        Assert.IsFalse(EveryoneAllowedRoutes.IsEveryoneAllowedRoute("microm", "app", "/microm/app/files"));
    }

    [TestMethod]
    public async Task CleanupService_QueuesImmediateDailySingleInstanceTask()
    {
        var queue = new Mock<IBackgroundTaskQueue>();
        var service = new FileStoreCleanupService(
            queue.Object,
            Mock.Of<IMicroMAppConfiguration>(),
            Mock.Of<IFileUploadService>(),
            NullLogger<FileStoreCleanupService>.Instance);

        await service.StartAsync(CancellationToken.None);

        queue.Verify(item => item.Enqueue(
            "FileStoreCleanupService.CleanupTerminalFiles",
            It.IsAny<Func<CancellationToken, Task<string>>>(),
            true,
            TimeSpan.FromHours(24),
            null), Times.Once);
    }

    [TestMethod]
    public void FileQueries_FilterUploadedAndSelectEveryCleanupStatus()
    {
        var listSql = ReadEmbeddedSql("fcc_brwFiles.sql");
        StringAssert.Contains(listSql, "c_statusvalue_id = 'Uploaded'");

        var cleanupSql = ReadEmbeddedSql("fst_qryTerminalFiles.sql");
        StringAssert.Contains(cleanupSql, "'Deleted'");
        StringAssert.Contains(cleanupSql, "'Cancelled'");
        StringAssert.Contains(cleanupSql, "'Failed'");
    }

    [TestMethod]
    public void PhysicalCleanup_EvictsDiskCacheAndDeletesOriginalAndAllThumbnails()
    {
        var root = Path.Combine(Path.GetTempPath(), $"microm-cleanup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var original = Path.Combine(root, "file-guid.jpg");
            var thumbnail150 = Path.Combine(root, "file-guid-thmb-150-75.jpg");
            var thumbnail300 = Path.Combine(root, "file-guid-thmb-300-90.jpg");
            var unrelated = Path.Combine(root, "other.jpg");
            File.WriteAllText(original, "original");
            File.WriteAllText(thumbnail150, "thumbnail");
            File.WriteAllText(thumbnail300, "thumbnail");
            File.WriteAllText(unrelated, "unrelated");

            var details = new FileDetails
            {
                vc_fileguid = "file-guid.jpg",
                vc_filefolder = "202608",
                bi_filesize = 8,
                fullPath = original
            };
            var diskCache = new Mock<IDiskFileCacheService>();
            diskCache.Setup(cache => cache.RemoveEntry("app", details)).Returns(true);
            var cleanup = new FilePhysicalCleanupService(diskCache.Object, NullLogger<FilePhysicalCleanupService>.Instance);

            Assert.IsTrue(cleanup.TryCleanup("app", details));
            diskCache.Verify(cache => cache.RemoveEntry("app", details), Times.Once);
            Assert.IsFalse(File.Exists(original));
            Assert.IsFalse(File.Exists(thumbnail150));
            Assert.IsFalse(File.Exists(thumbnail300));
            Assert.IsTrue(File.Exists(unrelated));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void AssertContractHasNoInternalId(Type contractType)
    {
        var forbidden = new[] { "FileId", "file_id", "c_file_id" };
        var properties = contractType.GetProperties().Select(property => property.Name).ToArray();
        foreach (var property in forbidden)
        {
            Assert.IsFalse(properties.Contains(property, StringComparer.OrdinalIgnoreCase), $"{contractType.Name} exposes {property}.");
        }
    }

    private static string ReadEmbeddedSql(string fileName)
    {
        var assembly = typeof(FileStore).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
