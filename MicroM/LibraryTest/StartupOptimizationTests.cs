using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MicroM.Configuration;
using MicroM.Configuration.Entities;
using MicroM.Core;
using MicroM.Data;
using MicroM.Database;
using MicroM.DataDictionary.Configuration;
using MicroM.DataDictionary.Entities;
using MicroM.Extensions;
using MicroM.Generators.SQLGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LibraryTest;

[TestClass]
public class StartupOptimizationTests
{
    [TestMethod]
    public async Task SchemaGroups_Send100Scripts_WithoutSplittingScripts()
    {
        var scripts = Enumerable.Range(0, 201).Select(i => "CREATE PROC p" + i + " AS SELECT N'quoted '' text';").ToList();
        var groups = await CaptureSchemaBatches(scripts);
        CollectionAssert.AreEqual(new[] { 100, 100, 1 }, groups.Select(g => g.Count).ToArray());
        CollectionAssert.AreEqual(scripts, groups.SelectMany(g => g).ToList());

        var large = new List<string> { new('x', 300000), new('y', 300000), new('z', 600000), "small" };
        groups = await CaptureSchemaBatches(large);
        CollectionAssert.AreEqual(new[] { 4 }, groups.Select(g => g.Count).ToArray());
        CollectionAssert.AreEqual(large, groups.SelectMany(g => g).ToList());
    }

    [TestMethod]
    public async Task SchemaGroups_EmptyInput_DoesNotSendCommands()
    {
        Assert.HasCount(0, await CaptureSchemaBatches([]));
    }

    private static List<string> ReadScriptLiterals(string body) => System.Text.RegularExpressions.Regex
        .Matches(body, @"EXEC\(N'((?:''|[^'])*)'\);", System.Text.RegularExpressions.RegexOptions.Singleline)
        .Select(match => match.Groups[1].Value.Replace("''", "'")).ToList();

    private static async Task<List<string>[]> CaptureSchemaBatches(List<string> texts)
    {
        var groups = new List<List<string>>();
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        client.Setup(c => c.ExecuteSQLNonQuery(It.Is<string>(sql => sql.StartsWith("BEGIN TRY", StringComparison.Ordinal)), It.IsAny<CancellationToken>()))
            .Callback((string body, CancellationToken ct) =>
            {
                StringAssert.Contains(body, "THROW;");
                Assert.IsFalse(body.Contains("sp_executesql", StringComparison.OrdinalIgnoreCase));
                groups.Add(ReadScriptLiterals(body));
            }).Returns(Task.CompletedTask);
        var custom = new CustomOrderedDictionary<CustomScript>();
        for (int i = 0; i < texts.Count; i++)
            custom.Add("p" + i, new("p" + i, "p", SQLScriptType.Procedure, SQLProcStandardType.Unknown, texts[i], "dbo"));
        await DatabaseSchema.CreateAllEntitiesProcs(client.Object,
            new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>(), custom, "dbo", false, CancellationToken.None);
        return groups.ToArray();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SchemaBatch_StopsAfterFailureOrCancellation(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        int calls = 0;
        client.Setup(c => c.ExecuteSQLNonQuery(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string body, CancellationToken ct) =>
            {
                calls++;
                if (cancel) cancellation.Cancel();
                else if (calls == 2) throw new InvalidOperationException("batch failure");
                return Task.CompletedTask;
            });
        var scripts = new CustomOrderedDictionary<CustomScript>();
        for (int i = 0; i < 201; i++)
            scripts.Add("p" + i, new("p" + i, "p", SQLScriptType.Procedure, SQLProcStandardType.Unknown, "CREATE PROC p" + i + " AS SELECT 1", "dbo"));
        Task Run() => DatabaseSchema.CreateAllEntitiesProcs(client.Object,
            new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>(), scripts, "dbo", false, cancellation.Token);
        if (cancel)
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(Run);
        else
        {
            var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(Run);
            Assert.AreEqual("batch failure", error.Message);
        }
        Assert.AreEqual(cancel ? 1 : 2, calls);
    }

    public class TestMenu : MenuDefinition
    {
        public readonly MenuItemDefinition parent = new("Parent's menu", allowed_routes: ["Entity/one", "Entity/two"]);
        public readonly MenuItemDefinition child = new("Child", nameof(parent), ["Entity/one"]);
        public TestMenu() : base("Menu's name") { }
    }

    [TestMethod]
    public async Task AddMenu_SendsOnePayload_PreservingHierarchyAndSharedRoutes()
    {
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        client.SetupGet(c => c.WebUser).Returns("audit-user");
        client.Setup(c => c.ExecuteSP<DBStatus>("[custom].mme_InitializeMenu", It.IsAny<CancellationToken>(), It.IsAny<AutoMapperMode>(),
                It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<IEntityClient.MapResult<DBStatus>>()))
            .Callback((string proc, CancellationToken ct, AutoMapperMode mode, IEnumerable<ColumnBase> parms, IEntityClient.MapResult<DBStatus> mapper) =>
            {
                var parameters = parms.ToArray();
                Assert.AreEqual("audit-user", parameters.Single(p => p.SQLParameterName == "webusr").ValueObject);
                using var json = JsonDocument.Parse((string)parameters.Single(p => p.SQLParameterName == "json_menu").ValueObject!);
                Assert.AreEqual("Menu's name", json.RootElement.GetProperty("menu_name").GetString());
                var items = json.RootElement.GetProperty("items").EnumerateArray().ToArray();
                Assert.HasCount(2, items);
                Assert.AreEqual("parent", items[0].GetProperty("item_id").GetString());
                Assert.AreEqual("parent", items[1].GetProperty("parent_id").GetString());
                Assert.AreEqual("/parent/child", items[1].GetProperty("item_path").GetString());
                Assert.AreEqual("Entity/one", items[0].GetProperty("routes")[0].GetString());
                Assert.AreEqual("Entity/one", items[1].GetProperty("routes")[0].GetString());
            }).ReturnsAsync(new List<DBStatus> { new(DBStatusCodes.OK, "OK") });

        await new TestMenu().AddMenu(client.Object, CancellationToken.None, "custom");
        client.Verify(c => c.ExecuteSP<DBStatus>(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<AutoMapperMode>(),
            It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<IEntityClient.MapResult<DBStatus>>()), Times.Once);
        client.Verify(c => c.BeginTransaction(It.IsAny<CancellationToken>()), Times.Never);
        client.Verify(c => c.Disconnect(), Times.Never);
    }

    [TestMethod]
    public async Task CustomProcedures_BatchByDependencyPhase_AndKeepFunctionsSeparate()
    {
        var scripts = new CustomOrderedDictionary<CustomScript>();
        scripts.Add("custom", new("x_custom", "x", SQLScriptType.Procedure, SQLProcStandardType.Unknown, "CUSTOM", "dbo"));
        scripts.Add("function", new("x_fn", "x", SQLScriptType.Function, SQLProcStandardType.Unknown, "FUNCTION", "dbo"));
        scripts.Add("idrop", new("x_idrop", "x", SQLScriptType.Procedure, SQLProcStandardType.IDrop, "IDROP", "dbo"));
        scripts.Add("iupdate", new("x_iupdate", "x", SQLScriptType.Procedure, SQLProcStandardType.IUpdate, "IUPDATE", "dbo"));
        scripts.Add("update", new("x_update", "x", SQLScriptType.Procedure, SQLProcStandardType.Update, "UPDATE", "dbo"));
        var batches = new List<string>();
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        client.Setup(c => c.ExecuteSQLNonQuery(It.Is<string>(sql => sql.StartsWith("BEGIN TRY", StringComparison.Ordinal)), It.IsAny<CancellationToken>()))
            .Callback((string body, CancellationToken ct) => batches.AddRange(ReadScriptLiterals(body)))
            .Returns(Task.CompletedTask);
        await DatabaseSchema.CreateAllEntitiesProcs(client.Object, new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>(), scripts, "dbo", false, CancellationToken.None);
        CollectionAssert.AreEqual(new[] { "IDROP", "IUPDATE", "UPDATE", "CUSTOM" }, batches);
        client.Verify(c => c.ExecuteSQLNonQuery("FUNCTION", It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.ExecuteSQLNonQuery(It.Is<List<string>>(s => s.Count > 0), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task GeneratedProcedures_AreBatched_BeforeCustomProcedures()
    {
        var batches = new List<string>();
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        client.Setup(c => c.ExecuteSQLNonQuery(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string body, CancellationToken ct) =>
            {
                StringAssert.StartsWith(body, "BEGIN TRY");
                batches.AddRange(ReadScriptLiterals(body));
            }).Returns(Task.CompletedTask);
        var entities = new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>();
        entities.Add(nameof(MicromMenus), new(new MicromMenus(client.Object, schema_name: "custom")));
        var scripts = new CustomOrderedDictionary<CustomScript>();
        scripts.Add("mme_get", new("mme_get", "mme", SQLScriptType.Procedure, SQLProcStandardType.Get, "CUSTOM GET", "custom"));
        scripts.Add("x_idrop", new("x_idrop", "x", SQLScriptType.Procedure, SQLProcStandardType.IDrop, "IDROP", "custom"));

        await DatabaseSchema.CreateAllEntitiesProcs(client.Object, entities, scripts, "custom", true, CancellationToken.None);

        int idrop = batches.IndexOf("IDROP");
        Assert.IsTrue(idrop > 0, "Generated procs must be batched before the IDrop phase");
        var generated = batches.Take(idrop).ToList();
        Assert.IsTrue(generated.Any(s => s.Contains("proc [custom].mme_drop", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(generated.Any(s => s.Contains("proc [custom].mme_get", StringComparison.OrdinalIgnoreCase)),
            "Generated get must be skipped when a custom one exists");
        Assert.AreEqual(1, batches.Count(s => s == "CUSTOM GET"));
    }

    [TestMethod]
    public async Task CreateSchemaAndProcs_AssignsConfiguredSchema_ToEntitiesWithoutSchema()
    {
        var sql = new List<string>();
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        client.Setup(c => c.ExecuteSQL(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<DataResult>());
        client.Setup(c => c.ExecuteSQLNonQuery(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string body, CancellationToken ct) => sql.Add(body)).Returns(Task.CompletedTask);
        var dd_entity = new MicromMenus(client.Object);
        var app_entity = new EntitiesAssemblies(client.Object);
        var entities = new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>();
        entities.Add(nameof(MicromMenus), new(dd_entity));
        entities.Add(nameof(EntitiesAssemblies), new(app_entity));

        await entities.CreateSchemaAndProcs(client.Object, new AppDBSchemaConfiguration("dd", "app"), CancellationToken.None, custom_procs: new CustomOrderedDictionary<CustomScript>());

        Assert.AreEqual("dd", dd_entity.Def.SchemaName);
        Assert.AreEqual("app", app_entity.Def.SchemaName);
        Assert.IsTrue(sql.Any(s => s.Contains($"create table [app].[{app_entity.Def.TableName}]", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task MenuProcedure_IsEmbedded_AndMatchesGeneratedHeaderParameters()
    {
        var scripts = await typeof(MicromMenus).Assembly.GetAllClassifiedCustomSQLScripts(CancellationToken.None, "custom");
        var script = scripts.Values.Single(s => s.ProcName == "mme_InitializeMenu");
        StringAssert.Contains(script.SQLText, "[custom].mme_update");
        StringAssert.Contains(script.SQLText, "[custom].mmi_iCreateItems");
        StringAssert.Contains(script.SQLText, "[custom].mir_iCreateItems");
        foreach (var name in new[] { "mmi_iCreateItems", "mir_iCreateItems" })
        {
            var helper = scripts.Values.Single(s => s.ProcName == name);
            StringAssert.Contains(helper.SQLText, "@json_items varchar(max)");
            StringAssert.Contains(helper.SQLText, "[custom].microm_menus_items");
        }
        var generated = new MicromMenus("custom").AsCreateUpdateProc("custom", true, false);
        StringAssert.Contains(string.Join("\n", generated), "@last_route_updated");
        Assert.HasCount(1, new MicromMenus("custom").Def.Procs.Values.Where(p => p.Name == "mme_InitializeMenu").ToArray());
    }

    public class TestCategory : CategoryDefinition
    {
        public readonly CategoryValuesDefinition ONE = new("Value's one");
        public readonly CategoryValuesDefinition TWO = new("Value two");
        public TestCategory() : base("Category's name") { }
    }

    public class TestNumericCategory : CategoryDefinition
    {
        public readonly CategoryValuesDefinition _1 = new("First");
        public TestNumericCategory() : base("Numeric category", numeric_ids: true) { }
    }

    public class TestStatus : StatusDefinition
    {
        public readonly StatusValuesDefinition PENDING = new("Pending", true);
        public readonly StatusValuesDefinition DONE = new("Done");
        public TestStatus() : base("Test status") { }
    }

    public class TestBatchEntityDef : EntityDefinition
    {
        public TestBatchEntityDef() : base("tbe", nameof(TestBatchEntity)) { }
        public readonly Column<string> c_test_id = Column<string>.PK(autonum: true);
    }

    public class TestBatchEntity : Entity<TestBatchEntityDef>
    {
        public TestBatchEntity() : base() { }
        public TestBatchEntity(IEntityClient ec, string? schema_name = null) : base(ec, null, schema_name) { }
    }

    private static Mock<IEntityClient> CaptureProcessPayloads(string proc_name, string json_parm, List<JsonDocument> payloads)
    {
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        client.SetupGet(c => c.WebUser).Returns("audit-user");
        client.Setup(c => c.ExecuteSP<DBStatus>(proc_name, It.IsAny<CancellationToken>(), It.IsAny<AutoMapperMode>(),
                It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<IEntityClient.MapResult<DBStatus>>()))
            .Callback((string proc, CancellationToken ct, AutoMapperMode mode, IEnumerable<ColumnBase> parms, IEntityClient.MapResult<DBStatus> mapper) =>
            {
                var parameters = parms.ToArray();
                Assert.AreEqual("audit-user", parameters.Single(p => p.SQLParameterName == "webusr").ValueObject);
                payloads.Add(JsonDocument.Parse((string)parameters.Single(p => p.SQLParameterName == json_parm).ValueObject!));
            }).ReturnsAsync(new List<DBStatus> { new(DBStatusCodes.OK, "OK") });
        return client;
    }

    private static void VerifySingleProcessCall(Mock<IEntityClient> client)
    {
        client.Verify(c => c.ExecuteSP<DBStatus>(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<AutoMapperMode>(),
            It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<IEntityClient.MapResult<DBStatus>>()), Times.Once);
        client.Verify(c => c.Disconnect(), Times.Never);
    }

    [TestMethod]
    public async Task AddCategories_SendsOnePayload_WithValuesAndNumericIds()
    {
        List<JsonDocument> payloads = [];
        var client = CaptureProcessPayloads("[custom].cat_InitializeCategories", "json_categories", payloads);

        await Array.Empty<CategoryDefinition>().AddCategories(client.Object, CancellationToken.None, "custom");
        await new CategoryDefinition[] { new TestCategory(), new TestNumericCategory(), new TestCategory() }.AddCategories(client.Object, CancellationToken.None, "custom");

        VerifySingleProcessCall(client);
        var categories = payloads.Single().RootElement.EnumerateArray().ToArray();
        Assert.HasCount(2, categories);
        Assert.AreEqual(nameof(TestCategory), categories[0].GetProperty("category_id").GetString());
        Assert.AreEqual("Category's name", categories[0].GetProperty("description").GetString());
        var values = categories[0].GetProperty("values").EnumerateArray().ToArray();
        Assert.HasCount(2, values);
        Assert.AreEqual("Value's one", values.Single(v => v.GetProperty("value_id").GetString() == "ONE").GetProperty("description").GetString());
        Assert.AreEqual("1", categories[1].GetProperty("values")[0].GetProperty("value_id").GetString());
    }

    [TestMethod]
    public async Task AddStatuses_SendsOnePayload_WithInitialValue()
    {
        List<JsonDocument> payloads = [];
        var client = CaptureProcessPayloads("[custom].sta_InitializeStatus", "json_status", payloads);

        await new StatusDefinition[] { new TestStatus() }.AddStatuses(client.Object, CancellationToken.None, "custom");

        VerifySingleProcessCall(client);
        var status = payloads.Single().RootElement.EnumerateArray().Single();
        Assert.AreEqual(nameof(TestStatus), status.GetProperty("status_id").GetString());
        Assert.AreEqual("Test status", status.GetProperty("description").GetString());
        var values = status.GetProperty("values").EnumerateArray().ToArray();
        Assert.HasCount(2, values);
        Assert.IsTrue(values.Single(v => v.GetProperty("value_id").GetString() == "PENDING").GetProperty("initial_value").GetBoolean());
        Assert.IsFalse(values.Single(v => v.GetProperty("value_id").GetString() == "DONE").GetProperty("initial_value").GetBoolean());
    }

    [TestMethod]
    public async Task AddEntitiesToDataDictionary_SendsOnePayload_WithNumberingCategoriesAndStatus()
    {
        List<JsonDocument> payloads = [];
        var client = CaptureProcessPayloads("[custom].obj_InitializeObjects", "json_objects", payloads);
        var entity = new TestBatchEntity(client.Object, "app");
        entity.Def.AddCategoryID(nameof(TestCategory));
        entity.Def.AddStatusID(nameof(TestStatus));
        var plain = new Categories(client.Object, schema_name: "custom");
        var entities = new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>();
        entities.TryAddEntities(true, entity, plain);

        await entities.AddEntitiesToDataDictionary(client.Object, CancellationToken.None, "custom");

        VerifySingleProcessCall(client);
        var objects = payloads.Single().RootElement.EnumerateArray().ToArray();
        Assert.HasCount(2, objects);
        Assert.AreEqual("tbe", objects[0].GetProperty("object_id").GetString());
        Assert.AreEqual(entity.Def.FullTableName, objects[0].GetProperty("table_name").GetString());
        Assert.IsTrue(objects[0].GetProperty("autonum").GetBoolean());
        Assert.AreEqual(nameof(TestCategory), objects[0].GetProperty("categories").EnumerateArray().Single().GetString());
        Assert.AreEqual(nameof(TestStatus), objects[0].GetProperty("status").EnumerateArray().Single().GetString());
        Assert.AreEqual("cat", objects[1].GetProperty("object_id").GetString());
        Assert.IsFalse(objects[1].GetProperty("autonum").GetBoolean());
        Assert.AreEqual(0, objects[1].GetProperty("categories").GetArrayLength());
    }

    [TestMethod]
    public async Task DataDictionaryProcedures_AreEmbedded_AndMatchDefinitions()
    {
        var scripts = await typeof(Objects).Assembly.GetAllClassifiedCustomSQLScripts(CancellationToken.None, "custom");
        foreach (var (name, parm) in new[] { ("cat_InitializeCategories", "@json_categories"), ("sta_InitializeStatus", "@json_status"), ("obj_InitializeObjects", "@json_objects") })
        {
            var script = scripts.Values.Single(s => s.ProcName == name);
            StringAssert.Contains(script.SQLText, $"create or alter proc [custom].{name}");
            StringAssert.Contains(script.SQLText, $"{parm} varchar(max)");
            Assert.IsFalse(script.SQLText.Contains("[dbo].", StringComparison.Ordinal));
        }
        Assert.HasCount(1, new Categories("custom").Def.Procs.Values.Where(p => p.Name == "cat_InitializeCategories").ToArray());
        Assert.HasCount(1, new Status("custom").Def.Procs.Values.Where(p => p.Name == "sta_InitializeStatus").ToArray());
        Assert.HasCount(1, new Objects("custom").Def.Procs.Values.Where(p => p.Name == "obj_InitializeObjects").ToArray());
    }

    [TestMethod]
    public async Task ConstraintsAndIndexes_UseOneQueryPerConstraintType_AndSkipExistingOnes()
    {
        var sql = new List<string>();
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        client.Setup(c => c.ExecuteSQLNonQuery(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string body, CancellationToken ct) => sql.Add(body)).Returns(Task.CompletedTask);

        var existing = new CategoriesValues("dd");
        var missing = new StatusValues("dd");
        var indexed = new ImportProcess("dd");
        var missing_indexes = new ApplicationOidcActiveSessions("dd");

        static List<DataResult> Rows(params object?[][] records)
        {
            var result = new DataResult(3);
            foreach (var record in records) result.records.Add(record);
            return [result];
        }

        client.Setup(c => c.ExecuteSQL(It.Is<string>(s => s.Contains("kc.type = 'PK'")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Rows([existing.Def.FullTableName.ToUpperInvariant(), "c_category_id,c_categoryvalue_id"]));
        client.Setup(c => c.ExecuteSQL(It.Is<string>(s => s.Contains("kc.type = 'UQ'")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Rows([existing.Def.FullTableName, "c_category_id,vc_description"]));
        client.Setup(c => c.ExecuteSQL(It.Is<string>(s => s.Contains("sys.foreign_keys")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Rows([existing.Def.FullTableName, "c_category_id"]));
        client.Setup(c => c.ExecuteSQL(It.Is<string>(s => s.Contains("sys.indexes")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Rows([indexed.Def.FullTableName, "vc_assemblytypename", false]));

        var entities = new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>();
        entities.TryAddEntities(true, existing, missing, indexed, missing_indexes);
        var schema_config = new AppDBSchemaConfiguration("dd", "app");

        await entities.CreateEntitiesConstraintsAndIndexes(schema_config, client.Object, CancellationToken.None);

        client.Verify(c => c.ExecuteSQL(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
        var ddl = string.Join("\n", sql);
        Assert.IsFalse(ddl.Contains($"ALTER TABLE {existing.Def.FullTableName} ADD CONSTRAINT", StringComparison.Ordinal), ddl);
        StringAssert.Contains(ddl, $"ALTER TABLE {missing.Def.FullTableName} ADD CONSTRAINT PK{missing.Def.Mneo} PRIMARY KEY");
        StringAssert.Contains(ddl, $"ALTER TABLE {missing.Def.FullTableName} ADD CONSTRAINT {missing.Def.Mneo}_UNDescription UNIQUE");
        StringAssert.Contains(ddl, $"ALTER TABLE {missing.Def.FullTableName} ADD CONSTRAINT {missing.Def.Mneo}_FKStates FOREIGN KEY");
        Assert.IsFalse(ddl.Contains($"create index {indexed.Def.Mneo}_IDXTableName", StringComparison.Ordinal), ddl);
        StringAssert.Contains(ddl, $"create index {missing_indexes.Def.Mneo}_IDXUserSessions");
        StringAssert.Contains(ddl, $"create index {missing_indexes.Def.Mneo}_IDXSub");

        int executed_ddl = sql.Count;
        await new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>().CreateEntitiesConstraintsAndIndexes(schema_config, client.Object, CancellationToken.None);
        client.Verify(c => c.ExecuteSQL(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
        Assert.HasCount(executed_ddl, sql);
    }

    private static CustomOrderedDictionary<CustomScript> CustomTypesTablesAndViews()
    {
        var scripts = new CustomOrderedDictionary<CustomScript>();
        scripts.Add("x_view1", new("x_view1", "x", SQLScriptType.View, SQLProcStandardType.Unknown, "VIEW 1", "dd"));
        scripts.Add("x_seq1", new("x_seq1", "x", SQLScriptType.Sequence, SQLProcStandardType.Unknown, "SEQUENCE 1", "dd"));
        scripts.Add("x_type1", new("x_type1", "x", SQLScriptType.Type, SQLProcStandardType.Unknown, "TYPE 1", "dd"));
        scripts.Add("x_table1", new("x_table1", "x", SQLScriptType.Table, SQLProcStandardType.Unknown, "TABLE 1", "dd"));
        scripts.Add("x_type2", new("x_type2", "x", SQLScriptType.Type, SQLProcStandardType.Unknown, "TYPE 2", "dd"));
        scripts.Add("x_table2", new("x_table2", "x", SQLScriptType.Table, SQLProcStandardType.Unknown, "TABLE 2", "dd"));
        scripts.Add("x_view2", new("x_view2", "x", SQLScriptType.View, SQLProcStandardType.Unknown, "VIEW 2", "dd"));
        return scripts;
    }

    private static readonly string[] CustomScriptsInExecutionOrder = ["TYPE 1", "TYPE 2", "SEQUENCE 1", "TABLE 1", "TABLE 2", "VIEW 1", "VIEW 2"];

    private static Mock<IEntityClient> CaptureNonQueries(List<string> sql)
    {
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        client.Setup(c => c.ExecuteSQL(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<DataResult>());
        client.Setup(c => c.ExecuteSQLNonQuery(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string body, CancellationToken ct) => sql.Add(body)).Returns(Task.CompletedTask);
        return client;
    }

    [TestMethod]
    public async Task CreateSchemaAndProcs_BatchesCustomTypesTablesAndViews()
    {
        var sql = new List<string>();
        var client = CaptureNonQueries(sql);
        var entities = new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>();
        entities.TryAddEntities(true, new Categories(client.Object, schema_name: "dd"));

        await entities.CreateSchemaAndProcs(client.Object, new AppDBSchemaConfiguration("dd", "app"), CancellationToken.None, custom_procs: CustomTypesTablesAndViews(), generate_standard_procs: false);

        var batches = sql.Where(s => s.StartsWith("BEGIN TRY", StringComparison.Ordinal)).Select(ReadScriptLiterals).ToList();
        Assert.HasCount(3, batches);
        CollectionAssert.AreEqual(new[] { "TYPE 1", "TYPE 2", "SEQUENCE 1" }, batches[0]);
        CollectionAssert.AreEqual(new[] { "TABLE 1", "TABLE 2" }, batches[1]);
        CollectionAssert.AreEqual(new[] { "VIEW 1", "VIEW 2" }, batches[2]);
        Assert.IsFalse(sql.Any(s => CustomScriptsInExecutionOrder.Contains(s)), "custom scripts must not be executed one by one");
    }

    [TestMethod]
    public async Task CreateSchemaAndProcsSequential_ExecutesCustomScriptsOneByOne()
    {
        var sql = new List<string>();
        var client = CaptureNonQueries(sql);
        var entities = new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>();
        entities.TryAddEntities(true, new Categories(client.Object, schema_name: "dd"));

        await entities.CreateSchemaAndProcsSequential(client.Object, new AppDBSchemaConfiguration("dd", "app"), CancellationToken.None, custom_procs: CustomTypesTablesAndViews(), generate_standard_procs: false);

        Assert.IsFalse(sql.Any(s => s.StartsWith("BEGIN TRY", StringComparison.Ordinal)));
        CollectionAssert.AreEqual(CustomScriptsInExecutionOrder, sql.Where(s => CustomScriptsInExecutionOrder.Contains(s)).ToList());
    }

    [TestMethod]
    public async Task CreateSchemaAndProcs_ConstraintErrorsOnCreatedTables_Throw()
    {
        var sql = new List<string>();
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        client.Setup(c => c.ExecuteSQL(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<DataResult>());
        client.Setup(c => c.ExecuteSQLNonQuery(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string body, CancellationToken ct) =>
            {
                if (body.Contains("ADD CONSTRAINT", StringComparison.Ordinal)) throw new InvalidOperationException("constraint failed");
                sql.Add(body);
            }).Returns(Task.CompletedTask);
        var entities = new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>();
        entities.TryAddEntities(true, new StatusValues(client.Object, schema_name: "dd"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => entities.CreateSchemaAndProcs(client.Object, new AppDBSchemaConfiguration("dd", "app"), CancellationToken.None, custom_procs: new CustomOrderedDictionary<CustomScript>()));
        Assert.IsFalse(sql.Any(s => s.StartsWith("BEGIN TRY", StringComparison.Ordinal)));
    }
}
