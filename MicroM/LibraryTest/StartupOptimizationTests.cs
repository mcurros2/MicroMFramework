using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
}
