using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.Entities;
using MicroM.Extensions;
using MicroM.Generators.SQLGenerator;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static LibraryTest.A_DatabaseClientTests;

namespace LibraryTest;

[TestClass]
[TestCategory("SQLIntegration")]
public class MenuBatchTests
{
    private readonly string _schema = "menu_batch_" + Guid.NewGuid().ToString("N");
    private string _connectionString = null!;
    private SqlConnection _connection = null!;

    [TestInitialize]
    public async Task CreateSchema()
    {
        using var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.TestDatabase,
            DatabaseConfiguration.user, DatabaseConfiguration.password);
        _connectionString = client.ConnectionString;
        _connection = new SqlConnection(_connectionString);
        await _connection.OpenAsync();
        await Execute($"create schema [{_schema}]");
        var schemaConfig = new AppDBSchemaConfiguration(_schema, _schema);
        EntityBase[] entities = [new Objects(_schema), new Numbering(_schema), new MicromMenus(_schema),
            new MicromRoutes(_schema), new MicromMenusItems(_schema), new MicromMenusItemsAllowedRoutes(_schema)];
        foreach (var entity in entities)
            foreach (var sql in entity.AsCreateTable(schemaConfig))
                await Execute(sql);

        await Execute($"""
            insert [{_schema}].objects values ('mro', 'mro', 'microm_routes', getdate(), getdate(), 'test', 'test', original_login(), original_login());
            insert [{_schema}].numbering values ('mro', 100, getdate(), getdate(), 'test', 'test', original_login(), original_login());
            """);
        foreach (var sql in new MicromMenus(_schema).AsCreateUpdateProc(_schema, true, false))
            await Execute(sql);
        var scripts = await typeof(MicromMenus).Assembly.GetAllClassifiedCustomSQLScripts(CancellationToken.None, _schema);
        foreach (var name in new[] { "mmi_iCreateItems", "mir_iCreateItems", "mme_InitializeMenu" })
            await Execute(scripts.Values.Single(s => s.ProcName == name).SQLText);
    }

    [TestCleanup]
    public async Task DropSchema()
    {
        if (_connection?.State == ConnectionState.Open)
        {
            try
            {
                await Execute($"""
                    if @@trancount > 0 rollback;
                    drop procedure if exists [{_schema}].mme_InitializeMenu, [{_schema}].mir_iCreateItems, [{_schema}].mmi_iCreateItems, [{_schema}].mme_update;
                    drop table if exists [{_schema}].microm_menus_items_allowed_routes;
                    drop table if exists [{_schema}].microm_menus_items;
                    drop table if exists [{_schema}].microm_routes;
                    drop table if exists [{_schema}].microm_menus;
                    drop table if exists [{_schema}].numbering;
                    drop table if exists [{_schema}].objects;
                    drop schema [{_schema}];
                    """);
            }
            finally { await _connection.DisposeAsync(); }
        }
    }

    private async Task Execute(string sql)
    {
        using var command = new SqlCommand(sql, _connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<object?> Scalar(string sql)
    {
        using var command = new SqlCommand(sql, _connection);
        return await command.ExecuteScalarAsync();
    }

    private async Task InitializeMenu(string json, SqlConnection? connection = null)
    {
        using var command = new SqlCommand($"[{_schema}].mme_InitializeMenu", connection ?? _connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add("@json_menu", SqlDbType.VarChar, -1).Value = json;
        command.Parameters.Add("@webusr", SqlDbType.VarChar, 255).Value = "audit-user";
        using var reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual(0, reader.GetInt32(0));
        Assert.IsFalse(await reader.NextResultAsync());
    }

    [TestMethod]
    public async Task InitializeMenu_BatchesHierarchySharedRoutesAndRepeatedInitialization()
    {
        const string menu = """
            {"menu_id":"main","menu_name":"Menu's name","items":[
                {"item_id":"child","parent_id":"parent","item_path":"/parent/child","item_name":"Child","routes":["Entity/one","Entity/one"]},
                {"item_id":"parent","parent_id":"","item_path":"/parent","item_name":"Parent","routes":["Entity/one","Entity/two"]}
            ]}
            """;
        await InitializeMenu(menu);
        Assert.AreEqual(2, await Scalar($"select count(*) from [{_schema}].microm_menus_items"));
        Assert.AreEqual(2, await Scalar($"select count(*) from [{_schema}].microm_routes"));
        Assert.AreEqual(3, await Scalar($"select count(*) from [{_schema}].microm_menus_items_allowed_routes"));
        Assert.AreEqual(102L, await Scalar($"select bi_lastnumber from [{_schema}].numbering where c_object_id = 'mro'"));
        Assert.AreEqual("0000000101", ((string)(await Scalar($"select c_route_id from [{_schema}].microm_routes where vc_route_path = 'Entity/one'"))!).TrimEnd());
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].microm_menus_items where c_menu_item_id = 'child' and c_parent_menu_id = 'main' and c_parent_item_id = 'parent' and vc_webinsuser = 'audit-user' and vc_insuser = original_login()"));
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].microm_menus_items where c_menu_item_id = 'parent' and c_parent_menu_id is null and c_parent_item_id is null"));
        var lastRouteUpdate = await Scalar($"select dt_last_route_updated from [{_schema}].microm_menus");
        Assert.IsInstanceOfType<DateTime>(lastRouteUpdate);

        await InitializeMenu(menu.Replace("Parent\"", "Changed\""));
        Assert.AreEqual(2, await Scalar($"select count(*) from [{_schema}].microm_menus_items"));
        Assert.AreEqual("Parent", await Scalar($"select vc_menu_item_name from [{_schema}].microm_menus_items where c_menu_item_id = 'parent'"));
        Assert.AreEqual(3, await Scalar($"select count(*) from [{_schema}].microm_menus_items_allowed_routes"));
        Assert.AreEqual(102L, await Scalar($"select bi_lastnumber from [{_schema}].numbering"));
        Assert.AreEqual(lastRouteUpdate, await Scalar($"select dt_last_route_updated from [{_schema}].microm_menus"));

        await InitializeMenu("""
            {"menu_id":"main","menu_name":"Changed","items":[
                {"item_id":"parent","item_path":"/ignored","item_name":"Ignored","routes":["Entity/one","Entity/three"]},
                {"item_id":"new","item_path":"/new","item_name":"New","routes":["Entity/three"]}
            ]}
            """);
        Assert.AreEqual(3, await Scalar($"select count(*) from [{_schema}].microm_routes"));
        Assert.AreEqual(5, await Scalar($"select count(*) from [{_schema}].microm_menus_items_allowed_routes"));
        Assert.AreEqual(103L, await Scalar($"select bi_lastnumber from [{_schema}].numbering"));
        Assert.AreEqual("Menu's name", await Scalar($"select vc_menu_name from [{_schema}].microm_menus"));
    }

    [TestMethod]
    [DataRow("{\"menu_id\":\"empty\",\"menu_name\":\"Empty\"}")]
    [DataRow("{\"menu_id\":\"empty\",\"menu_name\":\"Empty\",\"items\":[]}")]
    [DataRow("{\"menu_id\":\"empty\",\"menu_name\":\"Empty\",\"items\":[{\"item_id\":\"one\",\"item_path\":\"/one\",\"item_name\":\"One\",\"routes\":[]}]}")]
    public async Task InitializeMenu_EmptyArraysDoNotAllocateRouteNumbers(string json)
    {
        await InitializeMenu(json);
        Assert.AreEqual(0, await Scalar($"select count(*) from [{_schema}].microm_routes"));
        Assert.AreEqual(100L, await Scalar($"select bi_lastnumber from [{_schema}].numbering"));
        Assert.AreEqual(DBNull.Value, await Scalar($"select dt_last_route_updated from [{_schema}].microm_menus"));
    }

    [TestMethod]
    [DataRow("\"parent_id\":\"missing\",", "[]")]
    [DataRow("", "[null]")]
    public async Task InitializeMenu_InvalidParentOrRouteRollsBackHeaderAndItems(string parent, string routes)
    {
        var json = "{\"menu_id\":\"bad\",\"menu_name\":\"Bad\",\"items\":[{\"item_id\":\"one\"," + parent +
            "\"item_path\":\"/one\",\"item_name\":\"One\",\"routes\":" + routes + "}]}";
        await Assert.ThrowsAsync<SqlException>(() => InitializeMenu(json));
        Assert.AreEqual(0, await Scalar($"select count(*) from [{_schema}].microm_menus"));
        Assert.AreEqual(0, await Scalar($"select count(*) from [{_schema}].microm_menus_items"));
        Assert.AreEqual(0, await Scalar($"select count(*) from [{_schema}].microm_routes"));
        Assert.AreEqual(100L, await Scalar($"select bi_lastnumber from [{_schema}].numbering"));
        Assert.AreEqual(0, await Scalar("select @@trancount"));
    }

    [TestMethod]
    public async Task InitializeMenu_LinkFailureRollsBackRoutesAndNumbering()
    {
        await Execute($"alter table [{_schema}].microm_menus_items_allowed_routes add constraint RejectLinks check (c_menu_item_id <> 'blocked')");
        await Assert.ThrowsAsync<SqlException>(() => InitializeMenu("""
            {"menu_id":"bad","menu_name":"Bad","items":[
                {"item_id":"blocked","item_path":"/blocked","item_name":"Blocked","routes":["Entity/new"]}
            ]}
            """));
        Assert.AreEqual(0, await Scalar($"select count(*) from [{_schema}].microm_menus"));
        Assert.AreEqual(0, await Scalar($"select count(*) from [{_schema}].microm_menus_items"));
        Assert.AreEqual(0, await Scalar($"select count(*) from [{_schema}].microm_routes"));
        Assert.AreEqual(100L, await Scalar($"select bi_lastnumber from [{_schema}].numbering"));
    }

    [TestMethod]
    public async Task InitializeMenu_PathConflictsAndMissingNumberingFailWithoutPartialChanges()
    {
        await InitializeMenu("""{"menu_id":"main","menu_name":"Main","items":[{"item_id":"one","item_path":"/same","item_name":"One"}]}""");
        await Assert.ThrowsAsync<SqlException>(() => InitializeMenu("""{"menu_id":"main","menu_name":"Main","items":[{"item_id":"two","item_path":"/same","item_name":"Two"}]}"""));
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].microm_menus_items"));
        await Assert.ThrowsAsync<SqlException>(() => InitializeMenu("""
            {"menu_id":"main","menu_name":"Main","items":[
                {"item_id":"two","item_path":"/batch","item_name":"Two"},
                {"item_id":"three","item_path":"/batch","item_name":"Three"}
            ]}
            """));
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].microm_menus_items"));
        await Execute($"delete [{_schema}].numbering where c_object_id = 'mro'");
        await Assert.ThrowsAsync<SqlException>(() => InitializeMenu("""{"menu_id":"other","menu_name":"Other","items":[{"item_id":"two","item_path":"/two","item_name":"Two","routes":["Entity/new"]}]}"""));
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].microm_menus"));
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].microm_menus_items"));
    }

    [TestMethod]
    public async Task InitializeMenu_ConcurrentMenusShareRouteNumbersSafely()
    {
        await using var second = new SqlConnection(_connectionString);
        await second.OpenAsync();
        var firstJson = """{"menu_id":"first","menu_name":"First","items":[{"item_id":"one","item_path":"/one","item_name":"One","routes":["Entity/shared","Entity/first"]}]}""";
        var secondJson = firstJson.Replace("first", "second").Replace("First", "Second");
        await Task.WhenAll(InitializeMenu(firstJson), InitializeMenu(secondJson, second));
        Assert.AreEqual(3, await Scalar($"select count(*) from [{_schema}].microm_routes"));
        Assert.AreEqual(4, await Scalar($"select count(*) from [{_schema}].microm_menus_items_allowed_routes"));
        Assert.AreEqual(103L, await Scalar($"select bi_lastnumber from [{_schema}].numbering"));
    }

    [TestMethod]
    [DataRow("mmi_iCreateItems")]
    [DataRow("mir_iCreateItems")]
    public async Task InternalHelpers_RequireCallerTransaction(string procedure)
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => Execute($"exec [{_schema}].{procedure} @menu_id = 'main', @json_items = '[]', @webusr = 'test'"));
        Assert.AreEqual(50001, error.Number);
        Assert.AreEqual(0, await Scalar("select @@trancount"));
    }

    [TestMethod]
    public async Task InitializeMenu_ExecutePermissionCoversInternalHelpers()
    {
        var user = _schema + "_user";
        await Execute($"create user [{user}] without login; grant execute on [{_schema}].mme_InitializeMenu to [{user}]");
        try
        {
            await Execute($"execute as user = '{user}'");
            await InitializeMenu("""{"menu_id":"main","menu_name":"Main","items":[{"item_id":"one","item_path":"/one","item_name":"One","routes":["Entity/one"]}]}""");
        }
        finally { await Execute($"revert; drop user [{user}]"); }
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].microm_menus_items_allowed_routes"));
        Assert.AreEqual(101L, await Scalar($"select bi_lastnumber from [{_schema}].numbering"));
    }
}
