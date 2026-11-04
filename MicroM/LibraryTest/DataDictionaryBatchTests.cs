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
public class DataDictionaryBatchTests
{
    private readonly string _schema = "dd_batch_" + Guid.NewGuid().ToString("N");
    private SqlConnection _connection = null!;

    private const string CategoriesJson = """
        [{"category_id":"Colors","description":"Color's","values":[{"value_id":"RED","description":"Red"},{"value_id":"BLUE","description":"Blue"}]},
         {"category_id":"Sizes","description":"Sizes","values":[]}]
        """;

    private const string StatusJson = """
        [{"status_id":"Process","description":"Process","values":[{"value_id":"PENDING","description":"Pending","initial_value":true},{"value_id":"DONE","description":"Done","initial_value":false}]}]
        """;

    private const string ObjectsJson = """
        [{"object_id":"one","table_name":"[app].[one]","autonum":true,"categories":["Colors"],"status":["Process"]},
         {"object_id":"two","table_name":"[app].[two]","autonum":false,"categories":[],"status":[]}]
        """;

    [TestInitialize]
    public async Task CreateSchema()
    {
        using var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.TestDatabase,
            DatabaseConfiguration.user, DatabaseConfiguration.password);
        _connection = new SqlConnection(client.ConnectionString);
        await _connection.OpenAsync();
        await Execute($"create schema [{_schema}]");
        var schemaConfig = new AppDBSchemaConfiguration(_schema, _schema);
        EntityBase[] entities = [new Objects(_schema), new Numbering(_schema), new Categories(_schema), new CategoriesValues(_schema),
            new Status(_schema), new StatusValues(_schema), new ObjectsCategories(_schema), new ObjectsStatus(_schema)];
        foreach (var entity in entities)
            foreach (var sql in entity.AsCreateTable(schemaConfig))
                await Execute(sql);

        var scripts = await typeof(Objects).Assembly.GetAllClassifiedCustomSQLScripts(CancellationToken.None, _schema);
        foreach (var name in new[] { "cat_InitializeCategories", "sta_InitializeStatus", "obj_InitializeObjects" })
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
                    drop procedure if exists [{_schema}].cat_InitializeCategories, [{_schema}].sta_InitializeStatus, [{_schema}].obj_InitializeObjects;
                    drop table if exists [{_schema}].objects_status;
                    drop table if exists [{_schema}].objects_categories;
                    drop table if exists [{_schema}].status_values;
                    drop table if exists [{_schema}].status;
                    drop table if exists [{_schema}].categories_values;
                    drop table if exists [{_schema}].categories;
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

    private async Task Initialize(string procedure, string json_parm, string json)
    {
        using var command = new SqlCommand($"[{_schema}].{procedure}", _connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add(json_parm, SqlDbType.VarChar, -1).Value = json;
        command.Parameters.Add("@webusr", SqlDbType.VarChar, 255).Value = "audit-user";
        using var reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual(0, reader.GetInt32(0));
        Assert.IsFalse(await reader.NextResultAsync());
    }

    [TestMethod]
    public async Task InitializeCategories_InsertsOnce_AndKeepsExistingRows()
    {
        await Initialize("cat_InitializeCategories", "@json_categories", CategoriesJson);
        Assert.AreEqual(2, await Scalar($"select count(*) from [{_schema}].categories"));
        Assert.AreEqual(2, await Scalar($"select count(*) from [{_schema}].categories_values"));
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].categories_values where c_category_id = 'Colors' and c_categoryvalue_id = 'RED' and vc_webinsuser = 'audit-user' and vc_insuser = original_login()"));
        Assert.AreEqual("Color's", await Scalar($"select vc_description from [{_schema}].categories where c_category_id = 'Colors'"));

        await Initialize("cat_InitializeCategories", "@json_categories", CategoriesJson.Replace("\"Red\"", "\"Changed\""));
        Assert.AreEqual(2, await Scalar($"select count(*) from [{_schema}].categories"));
        Assert.AreEqual(2, await Scalar($"select count(*) from [{_schema}].categories_values"));
        Assert.AreEqual("Red", await Scalar($"select vc_description from [{_schema}].categories_values where c_categoryvalue_id = 'RED'"));

        await Initialize("cat_InitializeCategories", "@json_categories", """[{"category_id":"Sizes","description":"Sizes","values":[{"value_id":"S","description":"Small"}]}]""");
        Assert.AreEqual(3, await Scalar($"select count(*) from [{_schema}].categories_values"));
    }

    [TestMethod]
    public async Task InitializeStatus_InsertsOnce_WithInitialValue()
    {
        await Initialize("sta_InitializeStatus", "@json_status", StatusJson);
        await Initialize("sta_InitializeStatus", "@json_status", StatusJson.Replace("\"Pending\"", "\"Changed\""));
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].status"));
        Assert.AreEqual(2, await Scalar($"select count(*) from [{_schema}].status_values"));
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].status_values where c_statusvalue_id = 'PENDING' and bt_initial_value = 1"));
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].status_values where c_statusvalue_id = 'DONE' and bt_initial_value = 0"));
        Assert.AreEqual("Pending", await Scalar($"select vc_description from [{_schema}].status_values where c_statusvalue_id = 'PENDING'"));
    }

    [TestMethod]
    public async Task InitializeObjects_InsertsEachTableOnce_AndKeepsNumbering()
    {
        await Initialize("cat_InitializeCategories", "@json_categories", CategoriesJson);
        await Initialize("sta_InitializeStatus", "@json_status", StatusJson);

        await Initialize("obj_InitializeObjects", "@json_objects", ObjectsJson);
        Assert.AreEqual(2, await Scalar($"select count(*) from [{_schema}].objects"));
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].objects where c_object_id = 'one' and c_mneo_id = 'one' and vc_tablename = '[app].[one]' and vc_webinsuser = 'audit-user'"));
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].numbering"));
        Assert.AreEqual(0L, await Scalar($"select bi_lastnumber from [{_schema}].numbering where c_object_id = 'one'"));
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].objects_categories where c_object_id = 'one' and c_category_id = 'Colors'"));
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].objects_status where c_object_id = 'one' and c_status_id = 'Process'"));

        await Execute($"update [{_schema}].numbering set bi_lastnumber = 42 where c_object_id = 'one'");
        await Initialize("obj_InitializeObjects", "@json_objects", ObjectsJson.Replace("[app].[one]", "[app].[changed]"));
        Assert.AreEqual(2, await Scalar($"select count(*) from [{_schema}].objects"));
        Assert.AreEqual("[app].[one]", await Scalar($"select vc_tablename from [{_schema}].objects where c_object_id = 'one'"));
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].numbering"));
        Assert.AreEqual(42L, await Scalar($"select bi_lastnumber from [{_schema}].numbering where c_object_id = 'one'"));
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].objects_categories"));
        Assert.AreEqual(1, await Scalar($"select count(*) from [{_schema}].objects_status"));
    }

    [TestMethod]
    public async Task InitializeObjects_UnknownCategory_RollsBackAllTables()
    {
        await Initialize("sta_InitializeStatus", "@json_status", StatusJson);
        await Assert.ThrowsAsync<SqlException>(() => Initialize("obj_InitializeObjects", "@json_objects", ObjectsJson));
        Assert.AreEqual(0, await Scalar($"select count(*) from [{_schema}].objects"));
        Assert.AreEqual(0, await Scalar($"select count(*) from [{_schema}].numbering"));
        Assert.AreEqual(0, await Scalar($"select count(*) from [{_schema}].objects_categories"));
        Assert.AreEqual(0, await Scalar($"select count(*) from [{_schema}].objects_status"));
        Assert.AreEqual(0, await Scalar("select @@trancount"));
    }

    [TestMethod]
    [DataRow("cat_InitializeCategories", "@json_categories", """[{"category_id":"","description":"Empty","values":[]}]""")]
    [DataRow("cat_InitializeCategories", "@json_categories", """[{"category_id":"Colors","description":"Colors","values":[{"value_id":" ","description":"Empty"}]}]""")]
    [DataRow("sta_InitializeStatus", "@json_status", """[{"status_id":"Process","description":"Process","values":[{"value_id":"","description":"Empty","initial_value":false}]}]""")]
    [DataRow("obj_InitializeObjects", "@json_objects", """[{"object_id":"","table_name":"[app].[empty]","autonum":false,"categories":[],"status":[]}]""")]
    public async Task Initialize_EmptyIds_Throw(string procedure, string json_parm, string json)
    {
        var error = await Assert.ThrowsAsync<SqlException>(() => Initialize(procedure, json_parm, json));
        Assert.AreEqual(51000, error.Number);
        Assert.AreEqual(0, await Scalar($"select count(*) from [{_schema}].categories"));
        Assert.AreEqual(0, await Scalar($"select count(*) from [{_schema}].status"));
        Assert.AreEqual(0, await Scalar($"select count(*) from [{_schema}].objects"));
    }
}
