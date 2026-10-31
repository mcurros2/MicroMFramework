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
using MicroM.Extensions;
using MicroM.Generators.SQLGenerator;
using MicroM.Web.Authentication;
using MicroM.Web.Authentication.SSO;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static LibraryTest.A_DatabaseClientTests;

namespace LibraryTest;

[TestClass]
[DoNotParallelize]
[TestCategory("SQLIntegration")]
public class EntityCatalogPersistenceTests
{
    private readonly string _schema = "catalog_" + Guid.NewGuid().ToString("N");
    private readonly Mock<IBackgroundTaskQueue> _queue = new();
    private readonly Dictionary<string, Func<CancellationToken, Task<string>>> _work = [];
    private Dictionary<string, ApplicationOption> _apps = null!;
    private Dictionary<string, ApplicationOption> _savedApps = null!;
    private Dictionary<string, Type> _types = null!;
    private Dictionary<string, Type> _savedTypes = null!;
    private Dictionary<string, List<Assembly>> _assemblies = null!;
    private Dictionary<string, List<Assembly>> _savedAssemblies = null!;
    private MicroMAppConfigurationProvider _provider = null!;
    private SqlConnection _connection = null!;
    private Assembly _first = null!, _last = null!, _other = null!;

    private static T Cache<T>(string name) => (T)typeof(MicroMAppConfigurationProvider)
        .GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

    private static Assembly CreateAssembly(string entityName, string suffix)
    {
        var module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Catalog" + suffix + Guid.NewGuid().ToString("N")),
            AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule("entities");
        var entity = module.DefineType("Catalog" + suffix + "." + entityName, TypeAttributes.Public, typeof(MicromEntitiesTypes)).CreateType()!;
        module.DefineType("Catalog" + suffix + ".Initializer", TypeAttributes.Public, typeof(CatalogInitializerProbe)).CreateType();
        return entity.Assembly;
    }

    [TestInitialize]
    public async Task Initialize()
    {
        _apps = Cache<Dictionary<string, ApplicationOption>>("_ApplicationsCache");
        _types = Cache<Dictionary<string, Type>>("_EntityTypesCache");
        _assemblies = Cache<Dictionary<string, List<Assembly>>>("_EntityAssembliesCache");
        _savedApps = new(_apps);
        _savedTypes = new(_types);
        _savedAssemblies = new(_assemblies);
        _apps.Clear(); _types.Clear(); _assemblies.Clear();
        _first = CreateAssembly("CategoriasValores", "First");
        _last = CreateAssembly("Empresas", "Last");
        _other = CreateAssembly("OtherOnly", "Other");
        foreach (var (id, schema, dlls) in new[] { ("A", _schema, new[] { _first, _last }), ("B", _schema + "b", new[] { _first, _other }) })
        {
            _apps[id] = new ApplicationOption
            {
                ApplicationID = id, SQLServer = DatabaseConfiguration.Server, SQLDB = DatabaseConfiguration.TestDatabase,
                SQLUser = DatabaseConfiguration.user, SQLPassword = DatabaseConfiguration.password,
                SchemaConfiguration = new(schema, schema)
            };
            _assemblies[id] = dlls.ToList();
            foreach (var type in dlls.SelectMany(a => a.GetEntitiesTypes())) _types[id + "." + type.Key] = type.Value;
            _types[id + ".MicromUsers"] = typeof(MicromUsers);
        }
        _types["A1.OtherOnly"] = _other.GetEntitiesTypes().Values.Single();
        _queue.Setup(q => q.Enqueue(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<string>>>(), true, null, null))
            .Callback((string name, Func<CancellationToken, Task<string>> work, bool single, TimeSpan? recurrence, TimeSpan? delay) => _work[name] = work)
            .Returns(Guid.NewGuid());
        _provider = new MicroMAppConfigurationProvider(Options.Create(new MicroMOptions()),
            NullLogger<MicroMAppConfigurationProvider>.Instance, Mock.Of<IMicroMEncryption>(), _queue.Object,
            Mock.Of<IMemoryEventsService>(), new ConfigurationBuilder().Build(), Mock.Of<IAppAssemblyRuntimeManager>(),
            Mock.Of<IAssemblyShadowCopyService>(), Mock.Of<IHostApplicationLifetime>());

        using var client = _provider.GetDatabaseClient("A")!;
        _connection = new SqlConnection(client.ConnectionString);
        await _connection.OpenAsync();
        foreach (var schema in new[] { _schema, _schema + "b" })
        {
            await Execute($"create schema [{schema}]");
            foreach (var sql in new MicromEntitiesTypes(schema).AsCreateTable(new(schema, schema))) await Execute(sql);
            var scripts = await typeof(MicromEntitiesTypes).Assembly.GetAllClassifiedCustomSQLScripts(CancellationToken.None, schema);
            foreach (var name in new[] { "mty_GetEntitiesTypes", "mty_deleteAll", "mty_InsertChunk" })
                await Execute(scripts.Values.Single(s => s.ProcName == name).SQLText);
        }
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        try
        {
            if (_connection?.State == ConnectionState.Open)
                foreach (var schema in new[] { _schema, _schema + "b" })
                {
                    await Execute($"""
                        drop procedure if exists [{schema}].mty_GetEntitiesTypes, [{schema}].mty_deleteAll, [{schema}].mty_InsertChunk;
                        drop table if exists [{schema}].microm_entities_types;
                        drop schema [{schema}];
                        """);
                }
        }
        finally
        {
            if (_connection != null) await _connection.DisposeAsync();
            _apps.Clear(); foreach (var item in _savedApps) _apps[item.Key] = item.Value;
            _types.Clear(); foreach (var item in _savedTypes) _types[item.Key] = item.Value;
            _assemblies.Clear(); foreach (var item in _savedAssemblies) _assemblies[item.Key] = item.Value;
        }
    }

    private async Task Execute(string sql)
    {
        using var command = new SqlCommand(sql, _connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<string[]> Catalog(string schema)
    {
        using var command = new SqlCommand($"select vc_entity_name from [{schema}].microm_entities_types order by vc_entity_name", _connection);
        using var reader = await command.ExecuteReaderAsync();
        var names = new List<string>();
        while (await reader.ReadAsync()) names.Add(reader.GetString(0));
        return names.ToArray();
    }

    [TestMethod]
    public async Task BackgroundCatalog_AggregatesDlls_AndKeepsSharedAppsAndSnapshotsSeparate()
    {
        CollectionAssert.AreEqual(new[] { "CategoriasValores" }, _first.GetEntitiesTypes().Keys.ToArray());
        CollectionAssert.AreEqual(new[] { "Empresas" }, _last.GetEntitiesTypes().Keys.ToArray());
        _provider.QueuePersistAppEntityTypes("a");
        _provider.QueuePersistAppEntityTypes("B");
        Assert.HasCount(0, await Catalog(_schema));
        Assert.HasCount(2, _work);
        // Mutating the published cache afterward must not change the queued snapshot.
        _types.Clear();
        await _work["PersistAppEntityTypes.A"](CancellationToken.None);
        await _work["PersistAppEntityTypes.B"](CancellationToken.None);
        CollectionAssert.AreEqual(new[] { "CategoriasValores", "Empresas" }, await Catalog(_schema));
        CollectionAssert.AreEqual(new[] { "CategoriasValores", "OtherOnly" }, await Catalog(_schema + "b"));
        // An unchanged catalog does not delete/reinsert rows or collide on the key.
        await _work["PersistAppEntityTypes.A"](CancellationToken.None);
        CollectionAssert.AreEqual(new[] { "CategoriasValores", "Empresas" }, await Catalog(_schema));
    }

    [TestMethod]
    public async Task Initialization_UsesEachDllsOwnEntities_WithoutReadingOrWritingCatalog()
    {
        CatalogInitializerProbe.Seen.Clear();
        var client = new Mock<IEntityClient>(MockBehavior.Strict);
        client.Setup(c => c.Clone(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).Returns(client.Object);
        client.Setup(c => c.Connect(It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(true);
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        client.Setup(c => c.Disconnect()).Returns(Task.CompletedTask);
        client.Setup(c => c.Dispose());
        client.Setup(c => c.OverrideColumnValues(It.IsAny<IEnumerable<ColumnBase>>()));
        client.Setup(c => c.ExecuteSP<DBStatus>(It.Is<string>(s => s.EndsWith("mro_deleteAllRoutes", StringComparison.Ordinal)), It.IsAny<CancellationToken>(),
            It.IsAny<AutoMapperMode>(), It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<IEntityClient.MapResult<DBStatus>>()))
            .ReturnsAsync(new List<DBStatus> { new(DBStatusCodes.OK, "OK") });
        var method = typeof(ApplicationDatabase).GetMethod("InitializeApplication", BindingFlags.Static | BindingFlags.NonPublic)!;
        await (Task<List<IDatabaseSchema>>)method.Invoke(null,
            [client.Object, _apps["A"], "user", false, _provider, NullLogger.Instance, CancellationToken.None])!;
        Assert.HasCount(6, CatalogInitializerProbe.Seen);
        Assert.IsTrue(CatalogInitializerProbe.Seen.All(s => s.Types.All(t => t.Assembly == s.Assembly)));
        Assert.HasCount(0, _work);
        Assert.HasCount(0, await Catalog(_schema));
    }

    [TestMethod]
    public async Task RuntimeEntityCreation_WorksBeforeCatalogPersistence()
    {
        var service = new EntitiesService(Options.Create(new MicroMOptions()), NullLogger<WebAPIServices>.Instance,
            Mock.Of<IMicroMEncryption>(), _provider, _queue.Object, Mock.Of<IFileUploadService>(), Mock.Of<IEmailService>(),
            Mock.Of<ISecurityService>(), Mock.Of<IDeviceIdService>(), Mock.Of<IAuthenticationService>(), Mock.Of<IOIDCHttpClient>());
        using var client = _provider.GetDatabaseClient("A")!;
        var entity = service.CreateEntity(_apps["A"], "CategoriasValores", null, client);
        Assert.IsNotNull(entity);
        Assert.AreEqual(_first, entity.GetType().Assembly);
        Assert.HasCount(0, await Catalog(_schema));
    }

    [TestMethod]
    public async Task ExistingUser_CanLogInBeforeCatalogPersistence()
    {
        EntityBase[] tables = [new MicromUsers(_schema), new MicromUsersDevices(_schema), new MicromUsersGroupsMembers(_schema),
            new MicromUsersCat(_schema), new CategoriesValues(_schema), new MicromUsersLoginHistory(_schema), new Numbering(_schema)];
        string[] customProcs = ["num_iGetNewNumber", "usr_iupdate", "usd_iupdate", "usr_getUserData", "usr_updateLoginAttempt", "usr_GetClientClaims", "usr_GetServerClaims"];
        try
        {
            foreach (var entity in tables)
                foreach (var sql in entity.AsCreateTable(new(_schema, _schema), table_and_primary_key_only: true)) await Execute(sql);
            foreach (var entity in new EntityBase[] { tables[0], tables[5] })
                foreach (var sql in entity.AsCreateUpdateProc(_schema, create_or_alter: true, with_iupdate: true)) await Execute(sql);
            var scripts = await typeof(MicromUsers).Assembly.GetAllClassifiedCustomSQLScripts(CancellationToken.None, _schema);
            foreach (var name in customProcs)
            {
                var sql = scripts.Values.Single(s => string.Equals(s.ProcName, name, StringComparison.OrdinalIgnoreCase)).SQLText;
                // These two application extension points contain only comments by default.
                if (name.EndsWith("Claims", StringComparison.Ordinal)) sql += "\nreturn\n";
                await Execute(sql);
            }
            await Execute($"""
                insert [{_schema}].numbering values
                  ('usr', 0, getdate(), getdate(), 'test', 'test', original_login(), original_login()),
                  ('ulh', 0, getdate(), getdate(), 'test', 'test', original_login(), original_login());
                insert [{_schema}].categories_values values
                  ('UserTypes', 'ADMIN', 'Administrator', getdate(), getdate(), 'test', 'test', original_login(), original_login());
                """);
            using var client = _provider.GetDatabaseClient("A")!;
            var user = new MicromUsers(client, schema_name: _schema);
            user.Def.vc_username.Value = "catalog-admin";
            user.Def.vc_password.Value = "catalog-login-test-password";
            user.Def.c_usertype_id.Value = "ADMIN";
            await user.InsertData(CancellationToken.None, throw_dbstat_exception: true);

            var devices = new Mock<IDeviceIdService>();
            devices.Setup(d => d.GetDeviceID(It.IsAny<string>())).Returns(("catalog-device", "127.0.0.1", "catalog-test"));
            var authenticator = new MicroMAuthenticator(NullLogger<MicroMAuthenticator>.Instance, devices.Object,
                Mock.Of<IHttpContextAccessor>(), Options.Create(new MicroMOptions()), Mock.Of<IEmailService>(),
                Mock.Of<IMicroMEncryption>(), Mock.Of<ITwoFactorChallengeStore>(), Mock.Of<ITotpService>());
            Assert.HasCount(0, await Catalog(_schema));
            var result = await authenticator.AuthenticateLogin(_apps["A"], new UserLogin
            {
                Username = "catalog-admin", Password = "catalog-login-test-password", LocalDeviceID = "catalog-device"
            }, CancellationToken.None);
            Assert.AreEqual(PasswordVerificationResult.Success, result.PasswordVerificationResult);
            Assert.AreEqual("ADMIN", result.LoginData?.usertype_id);
            Assert.IsFalse(result.AccountDisabled || result.AccountLocked || result.RequiresTwoFactor);
            Assert.IsFalse(string.IsNullOrEmpty(result.LoginData?.refresh_token));
            Assert.HasCount(0, await Catalog(_schema));
            Assert.HasCount(0, _work);
        }
        finally
        {
            foreach (var name in customProcs.Concat(new[] { "usr_update", "ulh_update", "ulh_iupdate" }))
                await Execute($"drop procedure if exists [{_schema}].[{name}]");
            foreach (var entity in tables.Reverse()) await Execute($"drop table if exists {entity.Def.FullTableName}");
        }
    }

    [TestMethod]
    public void ControlPanelAndUnmappedApps_AreNotQueued()
    {
        _provider.QueuePersistAppEntityTypes(ConfigurationDefaults.ControlPanelAppID);
        _provider.QueuePersistAppEntityTypes("missing");
        Assert.HasCount(0, _work);
        Assert.IsNotNull(typeof(MicroMAppConfigurationProvider).GetMethod("PersistAppEntityTypes", BindingFlags.Instance | BindingFlags.NonPublic));
    }
}

public class CatalogInitializerProbe : IDatabaseSchema
{
    public static readonly List<(Assembly Assembly, Type[] Types)> Seen = [];
    private CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>? _entities;
    public Task<DatabaseMigrationResult> MigrateDatabase(IEntityClient ec, AppDBSchemaConfiguration schema, CancellationToken ct)
        => Task.FromResult(DatabaseMigrationResult.NoMigrationNeeded);
    public Task<CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>> GetEntitiesInstances(IEntityClient ec, AppDBSchemaConfiguration schema, CancellationToken ct)
    {
        _entities = GetType().Assembly.GetEntitiesInstances(ec, ct, schema_name: schema.APPSchema);
        return Task.FromResult(_entities);
    }
    private Task Check(CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities)
    {
        Assert.AreSame(_entities, entities);
        Seen.Add((GetType().Assembly, entities.Values.Select(e => e.EntityType).ToArray()));
        return Task.CompletedTask;
    }
    public Task CreateDBSchemaAndProcs(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, AppDBSchemaConfiguration schema, CancellationToken ct, bool create_or_alter = true, bool create_if_not_exists = true) => Check(entities);
    public Task GrantPermissions(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, string user, AppDBSchemaConfiguration schema, CancellationToken ct) => Check(entities);
    public Task CreateMenus(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, AppDBSchemaConfiguration schema, CancellationToken ct) => Check(entities);
    public Task SeedTestData(IEntityClient ec, AppDBSchemaConfiguration schema, string folder, CancellationToken ct) => Task.CompletedTask;
}
