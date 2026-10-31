using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Database;
using MicroM.DataDictionary.Entities;
using MicroM.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static LibraryTest.A_DatabaseClientTests;

namespace LibraryTest;

[TestClass]
[DoNotParallelize]
public class BackgroundSeedTestDataTests
{
    private readonly CancellationTokenSource _started = new();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Mock<IMemoryEventsService> _bus = new();
    private readonly Mock<IHostApplicationLifetime> _lifetime = new();
    private readonly Mock<IAppAssemblyRuntimeManager> _runtime = new();
    private Dictionary<string, ApplicationOption> _apps = null!;
    private Dictionary<string, ApplicationOption> _savedApps = null!;
    private BackgroundTaskQueue _queue = null!;
    private MicroMAppConfigurationProvider _provider = null!;

    [TestInitialize]
    public void Initialize()
    {
        _apps = (Dictionary<string, ApplicationOption>)typeof(MicroMAppConfigurationProvider)
            .GetField("_ApplicationsCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        _savedApps = new(_apps);
        _apps.Clear();
        _apps[ConfigurationDefaults.ControlPanelAppID] = new ApplicationOption
        {
            ApplicationID = ConfigurationDefaults.ControlPanelAppID,
            ApplicationName = "seed-source", SQLServer = DatabaseConfiguration.Server, SQLDB = DatabaseConfiguration.SystemDatabase,
            SQLUser = DatabaseConfiguration.user, SQLPassword = DatabaseConfiguration.password
        };
        _apps["A"] = new ApplicationOption
        {
            ApplicationID = "A", EnableSeedTestData = true, SQLServer = DatabaseConfiguration.Server,
            SQLDB = DatabaseConfiguration.TestDatabase, SchemaConfiguration = new("dbo", "dbo")
        };
        _lifetime.SetupGet(l => l.ApplicationStarted).Returns(_started.Token);
        _queue = new BackgroundTaskQueue(4, 100, NullLogger<BackgroundTaskQueue>.Instance, _shutdown.Token);
        _provider = CreateProvider(_queue);
    }

    private MicroMAppConfigurationProvider CreateProvider(IBackgroundTaskQueue queue) => new(
        Options.Create(new MicroMOptions { SecretsFilePath = System.IO.Path.GetTempPath(), SecretsFileName = Guid.NewGuid().ToString("N") }),
        NullLogger<MicroMAppConfigurationProvider>.Instance, Mock.Of<IMicroMEncryption>(), queue, _bus.Object,
        new ConfigurationBuilder().Build(), _runtime.Object, Mock.Of<IAssemblyShadowCopyService>(), _lifetime.Object);

    [TestCleanup]
    public async Task Cleanup()
    {
        _queue.CancelAllTasks();
        await WaitForJobs();
        _shutdown.Cancel();
        await _provider.StopAsync(CancellationToken.None);
        _queue.Dispose();
        _started.Dispose();
        _shutdown.Dispose();
        _apps.Clear();
        foreach (var app in _savedApps) _apps[app.Key] = app.Value;
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!condition()) await Task.Delay(10, timeout.Token);
    }

    private Task WaitForJobs() => WaitUntil(() => _queue.GetTasksStatus().Values.All(s =>
        s.Status is QueueTaskStatus.Completed or QueueTaskStatus.Cancelled or QueueTaskStatus.Failed));

    private static SqlConnection OwnedConnection(IEntityClient client) => (SqlConnection)typeof(DatabaseClient)
        .GetField("sql_connection", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(client)!;

    [TestMethod]
    [TestCategory("SQLIntegration")]
    public async Task Startup_SeedsOnlyAfterHostStarts_InInitializerOrder_WithOwnConnection()
    {
        var calls = new List<int>();
        SqlConnection? ownedConnection = null;
        var first = new Mock<IDatabaseSchema>();
        var second = new Mock<IDatabaseSchema>();
        first.Setup(i => i.SeedTestData(It.IsAny<IEntityClient>(), It.IsAny<AppDBSchemaConfiguration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((IEntityClient ec, AppDBSchemaConfiguration schema, string folder, CancellationToken ct) =>
            {
                Assert.AreEqual(ConnectionState.Open, ec.ConnectionState);
                Assert.AreEqual(DatabaseConfiguration.TestDatabase, ec.DB);
                Assert.AreEqual("seed-source", ec.ApplicationName);
                ownedConnection = OwnedConnection(ec);
                calls.Add(1);
            }).Returns(Task.CompletedTask);
        second.Setup(i => i.SeedTestData(It.IsAny<IEntityClient>(), It.IsAny<AppDBSchemaConfiguration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((IEntityClient ec, AppDBSchemaConfiguration schema, string folder, CancellationToken ct) => calls.Add(2)).Returns(Task.CompletedTask);

        _provider.QueueSeedTestData("A", [first.Object, second.Object]);
        Assert.HasCount(0, _queue.GetTasksStatus());
        Assert.HasCount(0, calls);
        _started.Cancel();
        await WaitForJobs();
        CollectionAssert.AreEqual(new[] { 1, 2 }, calls);
        Assert.AreEqual(ConnectionState.Closed, ownedConnection!.State);
        Assert.AreEqual(QueueTaskStatus.Completed, _queue.GetTasksStatus().Values.Single().Status);
        first.Verify(i => i.MigrateDatabase(It.IsAny<IEntityClient>(), It.IsAny<AppDBSchemaConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
        first.Verify(i => i.GetEntitiesInstances(It.IsAny<IEntityClient>(), It.IsAny<AppDBSchemaConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
        _bus.Verify(b => b.Publish(It.Is<AppDatabaseUpdatedOnHotReloadEvent>(e => e.ApplicationID == "A")), Times.Once);
    }

    [TestMethod]
    [TestCategory("SQLIntegration")]
    public async Task SingleInstance_PreventsDuplicateJobs_AndAllowsDifferentAppsToRunConcurrently()
    {
        _started.Cancel();
        _apps["B"] = new ApplicationOption
        {
            ApplicationID = "B", EnableSeedTestData = true, SQLServer = _apps["A"].SQLServer,
            SQLDB = _apps["A"].SQLDB, SchemaConfiguration = _apps["A"].SchemaConfiguration
        };
        var running = 0;
        var seed = new Mock<IDatabaseSchema>();
        seed.Setup(i => i.SeedTestData(It.IsAny<IEntityClient>(), It.IsAny<AppDBSchemaConfiguration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (IEntityClient ec, AppDBSchemaConfiguration schema, string folder, CancellationToken ct) =>
            {
                Interlocked.Increment(ref running);
                await Task.Delay(Timeout.Infinite, ct);
            });
        _provider.QueueSeedTestData("A", [seed.Object]);
        await WaitUntil(() => Volatile.Read(ref running) == 1);
        _provider.QueueSeedTestData("a", [seed.Object]);
        Assert.HasCount(1, _queue.GetTasksStatus());
        _provider.QueueSeedTestData("B", [seed.Object]);
        await WaitUntil(() => Volatile.Read(ref running) == 2);
        Assert.HasCount(2, _queue.GetTasksStatus());
        _queue.CancelAllTasks();
        await WaitForJobs();
        Assert.IsTrue(_queue.GetTasksStatus().Values.All(s => s.Status == QueueTaskStatus.Cancelled));
    }

    [TestMethod]
    [TestCategory("SQLIntegration")]
    public async Task Refresh_DoesNotWaitForSeed_QueueCancellationDisposesConnection()
    {
        _started.Cancel();
        var running = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        SqlConnection? ownedConnection = null;
        var seed = new Mock<IDatabaseSchema>();
        seed.Setup(i => i.SeedTestData(It.IsAny<IEntityClient>(), It.IsAny<AppDBSchemaConfiguration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (IEntityClient ec, AppDBSchemaConfiguration schema, string folder, CancellationToken ct) =>
            {
                ownedConnection = OwnedConnection(ec);
                running.SetResult();
                await Task.Delay(Timeout.Infinite, ct);
            });
        _provider.QueueSeedTestData("A", [seed.Object]);
        await running.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.IsFalse(await _provider.RefreshConfiguration("A", CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10)));
        _runtime.Verify(r => r.DisableFileWatchers(), Times.Once);
        Assert.AreEqual(ConnectionState.Open, ownedConnection!.State);
        Assert.AreEqual(QueueTaskStatus.Running, _queue.GetTasksStatus().Values.Single().Status);
        _queue.CancelTask(_queue.GetTasksStatus().Keys.Single());
        await WaitForJobs();
        Assert.AreEqual(ConnectionState.Closed, ownedConnection.State);
        Assert.AreEqual(QueueTaskStatus.Cancelled, _queue.GetTasksStatus().Values.Single().Status);
        _bus.Verify(b => b.Publish(It.IsAny<AppDatabaseUpdatedOnHotReloadEvent>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("SQLIntegration")]
    public async Task Connection_IsCreatedInsideQueuedDelegate_UsingCurrentConfiguration()
    {
        var seed = new Mock<IDatabaseSchema>();
        _started.Cancel();
        Func<CancellationToken, Task<string>>? work = null;
        var queue = new Mock<IBackgroundTaskQueue>();
        queue.Setup(q => q.Enqueue("SeedTestData.A", It.IsAny<Func<CancellationToken, Task<string>>>(), true, null, null))
            .Callback((string name, Func<CancellationToken, Task<string>> worker, bool single, TimeSpan? recurrence, TimeSpan? delay) => work = worker)
            .Returns(Guid.NewGuid());
        var provider = CreateProvider(queue.Object);
        try
        {
            // No usable source connection exists while the task is being queued.
            _apps[ConfigurationDefaults.ControlPanelAppID].SQLServer = "";
            provider.QueueSeedTestData("A", [seed.Object]);
            seed.Verify(i => i.SeedTestData(It.IsAny<IEntityClient>(), It.IsAny<AppDBSchemaConfiguration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _apps[ConfigurationDefaults.ControlPanelAppID].SQLServer = DatabaseConfiguration.Server;
            SqlConnection? ownedConnection = null;
            seed.Setup(i => i.SeedTestData(It.IsAny<IEntityClient>(), It.IsAny<AppDBSchemaConfiguration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback((IEntityClient ec, AppDBSchemaConfiguration schema, string folder, CancellationToken ct) =>
                {
                    ownedConnection = OwnedConnection(ec);
                    Assert.AreEqual("seed-source", ec.ApplicationName);
                    Assert.AreEqual(DatabaseConfiguration.TestDatabase, ec.DB);
                    Assert.AreEqual(ConnectionState.Open, ec.ConnectionState);
                }).Returns(Task.CompletedTask);
            await work!(CancellationToken.None);
            Assert.AreEqual(ConnectionState.Closed, ownedConnection!.State);
            queue.Verify(q => q.CancelTask(It.IsAny<Guid>()), Times.Never);
        }
        finally { await provider.StopAsync(CancellationToken.None); }
    }

    [TestMethod]
    [TestCategory("SQLIntegration")]
    public async Task FailedSeed_IsReportedByQueue_AndClosesItsConnection()
    {
        _started.Cancel();
        SqlConnection? ownedConnection = null;
        var seed = new Mock<IDatabaseSchema>();
        seed.Setup(i => i.SeedTestData(It.IsAny<IEntityClient>(), It.IsAny<AppDBSchemaConfiguration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((IEntityClient ec, AppDBSchemaConfiguration schema, string folder, CancellationToken ct) => ownedConnection = OwnedConnection(ec))
            .ThrowsAsync(new InvalidOperationException("seed-failure-marker"));
        _provider.QueueSeedTestData("A", [seed.Object]);
        await WaitForJobs();
        var status = _queue.GetTasksStatus().Values.Single();
        Assert.AreEqual(QueueTaskStatus.Failed, status.Status);
        StringAssert.Contains(status.StatusMessage!, "seed-failure-marker");
        Assert.AreEqual(ConnectionState.Closed, ownedConnection!.State);
        _bus.Verify(b => b.Publish(It.IsAny<AppDatabaseUpdatedOnHotReloadEvent>()), Times.Never);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, true)]
    public void DisabledOrEmptySeedWork_IsNotQueued(bool enabled, bool empty)
    {
        _apps["A"].EnableSeedTestData = enabled;
        _started.Cancel();
        _provider.QueueSeedTestData("A", empty ? [] : [Mock.Of<IDatabaseSchema>()]);
        Assert.HasCount(0, _queue.GetTasksStatus());
    }

    [TestMethod]
    [DataRow(true, true, DatabaseMigrationResult.NoMigrationNeeded, 1)]
    [DataRow(false, true, DatabaseMigrationResult.NoMigrationNeeded, 0)]
    [DataRow(true, false, DatabaseMigrationResult.NoMigrationNeeded, 0)]
    [DataRow(true, true, DatabaseMigrationResult.Migrated, 0)]
    [DataRow(true, true, DatabaseMigrationResult.NotMigrated, 0)]
    public async Task Initialization_CollectsEligibleSeeds_WithoutExecutingThem(bool enabled, bool seedTestData, DatabaseMigrationResult migration, int expected)
    {
        SeedInitializerFixture.Migration = migration;
        SeedInitializerFixture.FailMenus = false;
        _apps["A"].EnableSeedTestData = enabled;
        var eligible = await InitializeApplication(seedTestData);
        Assert.HasCount(expected, eligible);
        Assert.IsFalse(SeedInitializerFixture.Last!.Calls.Contains("Seed"));
        Assert.AreEqual("Menus", SeedInitializerFixture.Last.Calls.Last());
        Assert.HasCount(0, _queue.GetTasksStatus());
        if (expected > 0) Assert.AreSame(SeedInitializerFixture.Last, eligible[0]);
    }

    [TestMethod]
    public async Task FailedInitialization_DoesNotSeedOrQueuePartialWork()
    {
        SeedInitializerFixture.Migration = DatabaseMigrationResult.NoMigrationNeeded;
        SeedInitializerFixture.FailMenus = true;
        try
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => InitializeApplication(true));
            Assert.IsFalse(SeedInitializerFixture.Last!.Calls.Contains("Seed"));
            Assert.HasCount(0, _queue.GetTasksStatus());
        }
        finally { SeedInitializerFixture.FailMenus = false; }
    }

    private async Task<List<IDatabaseSchema>> InitializeApplication(bool seedTestData)
    {
        var client = new Mock<IEntityClient>();
        client.Setup(c => c.Clone(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).Returns(client.Object);
        client.Setup(c => c.Connect(It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(true);
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        client.Setup(c => c.ExecuteSP("[dbo].mty_GetEntitiesTypes", It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Foreground initialization must not read or replace the entity catalog."));
        client.Setup(c => c.ExecuteSP<DBStatus>(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<AutoMapperMode>(),
            It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<IEntityClient.MapResult<DBStatus>>()))
            .ReturnsAsync(new List<DBStatus> { new(DBStatusCodes.OK, "OK") });
        var config = new Mock<IMicroMAppConfiguration>();
        var module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("SeedFixture" + Guid.NewGuid().ToString("N")),
            AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule("initializer");
        module.DefineType("Initializer", TypeAttributes.Public, typeof(SeedInitializerFixture)).CreateType();
        config.Setup(c => c.GetAllAPPAssemblies("A")).Returns([module.Assembly]);
        var method = typeof(ApplicationDatabase).GetMethod("InitializeApplication", BindingFlags.Static | BindingFlags.NonPublic)!;
        return await (Task<List<IDatabaseSchema>>)method.Invoke(null,
            [client.Object, _apps["A"], "user", seedTestData, config.Object, NullLogger.Instance, CancellationToken.None])!;
    }
}

public class SeedInitializerFixture : IDatabaseSchema
{
    public static DatabaseMigrationResult Migration;
    public static bool FailMenus;
    public static SeedInitializerFixture? Last;
    public readonly List<string> Calls = [];

    public SeedInitializerFixture() => Last = this;

    public Task<DatabaseMigrationResult> MigrateDatabase(IEntityClient ec, AppDBSchemaConfiguration schema, CancellationToken ct)
    {
        Calls.Add("Migrate");
        return Task.FromResult(Migration);
    }

    public Task<CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>> GetEntitiesInstances(IEntityClient ec, AppDBSchemaConfiguration schema, CancellationToken ct)
    {
        Calls.Add("Entities");
        return Task.FromResult(new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>()
            .Add(nameof(MicromEntitiesTypes), new(new MicromEntitiesTypes(ec, schema_name: schema.DDSchema))));
    }

    public Task CreateDBSchemaAndProcs(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, AppDBSchemaConfiguration schema, CancellationToken ct, bool create_or_alter = true, bool create_if_not_exists = true)
    {
        Calls.Add("Schema");
        return Task.CompletedTask;
    }

    public Task GrantPermissions(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, string user, AppDBSchemaConfiguration schema, CancellationToken ct)
    {
        Calls.Add("Permissions");
        return Task.CompletedTask;
    }

    public Task CreateMenus(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, AppDBSchemaConfiguration schema, CancellationToken ct)
    {
        Calls.Add("Menus");
        if (FailMenus) throw new InvalidOperationException("menu-failure-marker");
        return Task.CompletedTask;
    }

    public Task SeedTestData(IEntityClient ec, AppDBSchemaConfiguration schema, string folder, CancellationToken ct)
    {
        Calls.Add("Seed");
        return Task.CompletedTask;
    }
}
