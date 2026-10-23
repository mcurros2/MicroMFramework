using MicroM.AutomatedTests;
using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryTest;

[TestClass]
public class EmptyLookupTests
{
    private string folder = null!;
    private Mock<IEntityClient> client = null!;
    private EmptyLookupTestEntity entity = null!;
    private readonly List<string> messages = [];
    private const string EmptyDefinition = "CREATE PROC dbo.elkt_lookup @lookup_id char(20) AS BEGIN /* compatibility stub */ END;";
    private const string DefinitionQuery = "SELECT OBJECT_DEFINITION(OBJECT_ID('[custom].elkt_lookup', N'P'))";

    [TestInitialize]
    public void Setup()
    {
        folder = Path.Combine(Path.GetTempPath(), $"microm-empty-lookup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        client = new Mock<IEntityClient>(MockBehavior.Strict);
        entity = new EmptyLookupTestEntity();
        entity.Init(client.Object, schema_name: "custom");
        messages.Clear();
        WriteFixture(1);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(folder, recursive: true);

    [TestMethod]
    [DataRow("CREATE PROC p AS -- comment only")]
    [DataRow("CREATE PROCEDURE [dbo].[p] AS /* comment */ ;")]
    [DataRow("ALTER PROC p AS BEGIN END")]
    [DataRow("CREATE OR ALTER PROC p AS BEGIN; BEGIN /* nested /* comment */ tail */ END; END;;")]
    [DataRow("/* header */ create /* separator */ proc p @id int as\n-- first\r\n/* second */ begin end")]
    [DataRow("CREATE PROC p @text varchar(80) = 'AS -- /* */', @n int = 1 AS BEGIN END")]
    [DataRow("CREATE PROC [AS--/*]]p] @text varchar(80) = N'it''s AS' AS BEGIN END")]
    [DataRow("CREATE PROC \"AS--/*\" @text varchar(80) = 'AS' WITH EXECUTE AS 'owner' AS BEGIN END")]
    [DataRow("CREATE PROC p WITH RECOMPILE, EXECUTE AS OWNER AS BEGIN END")]
    [DataRow("CREATE PROC p @id AS int, @text AS varchar(80) = 'AS' WITH EXECUTE AS OWNER AS BEGIN END")]
    public void ClassifyBody_RecognizesEmptyProcedures(string definition)
    {
        Assert.AreEqual(LookupProcedureBody.Empty, LookupProcedureInspection.ClassifyBody(definition));
    }

    [TestMethod]
    [DataRow("SELECT 1")]
    [DataRow("SELECT NULL")]
    [DataRow("SELECT 1 WHERE 1 = 0")]
    [DataRow("RETURN")]
    [DataRow("SET NOCOUNT ON")]
    [DataRow("DECLARE @x int")]
    [DataRow("EXEC('SELECT 1')")]
    [DataRow("IF 1 = 0 BEGIN SELECT 1 END")]
    [DataRow("BEGIN TRY SELECT 1 END TRY BEGIN CATCH END CATCH")]
    [DataRow("SELECT '-- /* AS BEGIN END */'")]
    [DataRow("SELECT [--/*BEGIN END AS]]column]")]
    [DataRow("SELECT \"-- /* AS\"")]
    [DataRow("BE/* must not concatenate tokens */GIN END")]
    [DataRow("[BEGIN] [END]")]
    [DataRow("'-- only a string'")]
    public void ClassifyBody_RejectsExecutableOrOtherContent(string body)
    {
        Assert.AreEqual(LookupProcedureBody.NonEmpty, LookupProcedureInspection.ClassifyBody($"CREATE PROC p AS {body}"));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    [DataRow("-- only a comment")]
    [DataRow("SELECT 1")]
    [DataRow("CREATE TABLE p (id int)")]
    [DataRow("CREATE OR PROC p AS BEGIN END")]
    [DataRow("CREATE PROC AS BEGIN END")]
    [DataRow("CREATE PROC p")]
    [DataRow("CREATE PROC p WITH EXECUTE AS OWNER")]
    [DataRow("CREATE PROC p AS BEGIN")]
    [DataRow("CREATE PROC p AS END BEGIN")]
    [DataRow("CREATE PROC p AS /* unclosed")]
    [DataRow("CREATE PROC p AS /* outer /* closed inner */")]
    [DataRow("CREATE PROC p AS 'unclosed")]
    [DataRow("CREATE PROC p AS [unclosed")]
    [DataRow("CREATE PROC p AS \"unclosed")]
    public void ClassifyBody_DoesNotApproveUnrecognizedDefinitions(string? definition)
    {
        Assert.AreEqual(LookupProcedureBody.Unknown, LookupProcedureInspection.ClassifyBody(definition));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task FullTest_DefaultStillFailsEmptyLookupWithoutInspecting(string? lookupResult)
    {
        SetupLookup(lookupResult);
        var ex = await Assert.ThrowsExactlyAsync<Exception>(() => EntityTests.FullTest(entity, folder, false, CancellationToken.None));
        StringAssert.Contains(ex.Message, "LOOKUP: Can't lookup record after insertion.");
        Assert.AreEqual(0, entity.UpdateCount);
        Assert.AreEqual(0, entity.DeleteCount);
        client.VerifyNoOtherCallsExceptLookup();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task FullTest_SuccessfulLookupDoesNotInspect(bool skipEmptyLookups)
    {
        SetupLookup("Description");
        await EntityTests.FullTest(entity, folder, false, CancellationToken.None, messages.Add, skipEmptyLookups);
        Assert.AreEqual(1, entity.UpdateCount);
        Assert.AreEqual(1, entity.DeleteCount);
        Assert.IsFalse(messages.Any(message => message.Contains("Skipping lookup assertion")));
        client.VerifyNoOtherCallsExceptLookup();
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task FullTest_EmptyProcedureLogsAndContinuesCrudAndSeeding(string? lookupResult)
    {
        SetupLookup(lookupResult);
        SetupDefinition(EmptyDefinition);
        using var cts = new CancellationTokenSource();

        await EntityTests.FullTest(entity, folder, true, cts.Token, messages.Add, skip_empty_lookups: true);

        Assert.AreEqual(2, entity.InsertCount);
        Assert.AreEqual(1, entity.UpdateCount);
        Assert.AreEqual(1, entity.DeleteCount);
        Assert.AreEqual(3, entity.GetCount);
        string message = messages.Single(message => message.Contains("Skipping lookup assertion"));
        StringAssert.Contains(message, entity.Def.Name);
        StringAssert.Contains(message, "[custom].elkt_lookup");
        StringAssert.Contains(message, "Record 1");
        StringAssert.Contains(message, "c_lookup_id: 1");
        client.Verify(c => c.ExecuteSQLSingleColumn<string>(DefinitionQuery, cts.Token, null), Times.Once);
    }

    [TestMethod]
    public async Task FullTest_InspectsEachFailedRecordIndependently()
    {
        WriteFixture(2);
        SetupLookup(null);
        SetupDefinition(EmptyDefinition);
        await EntityTests.FullTest(entity, folder, false, CancellationToken.None, messages.Add, skip_empty_lookups: true);
        Assert.AreEqual(2, entity.UpdateCount);
        Assert.AreEqual(2, entity.DeleteCount);
        Assert.HasCount(2, messages.Where(message => message.Contains("Skipping lookup assertion")).ToList());
        client.Verify(c => c.ExecuteSQLSingleColumn<string>(DefinitionQuery, CancellationToken.None, null), Times.Exactly(2));
    }

    [TestMethod]
    public async Task FullTest_SkippingLookupDoesNotSuppressLaterFailures()
    {
        SetupLookup(null);
        SetupDefinition(EmptyDefinition);
        entity.AdvanceLastUpdateOnUpdate = false;

        var ex = await Assert.ThrowsExactlyAsync<Exception>(() => EntityTests.FullTest(entity, folder, false, CancellationToken.None, messages.Add, skip_empty_lookups: true));

        StringAssert.Contains(ex.Message, "UPDATE: The column dt_lu was not updated");
        Assert.IsTrue(messages.Any(message => message.Contains("Skipping lookup assertion")));
        Assert.AreEqual(1, entity.UpdateCount);
        Assert.AreEqual(0, entity.DeleteCount);
    }

    [TestMethod]
    [DataRow("CREATE PROC p AS SELECT NULL", "executable content")]
    [DataRow("CREATE PROC p AS RETURN", "executable content")]
    [DataRow("CREATE PROC p AS SET NOCOUNT ON", "executable content")]
    [DataRow(null, "emptiness could not be verified")]
    [DataRow("", "emptiness could not be verified")]
    [DataRow("not a procedure", "emptiness could not be verified")]
    public async Task FullTest_UnverifiedOrNonemptyBodyStillFails(string? definition, string reason)
    {
        SetupLookup(null);
        SetupDefinition(definition);
        var ex = await Assert.ThrowsExactlyAsync<Exception>(() => EntityTests.FullTest(entity, folder, false, CancellationToken.None, messages.Add, skip_empty_lookups: true));
        StringAssert.Contains(ex.Message, "LOOKUP: Can't lookup record");
        StringAssert.Contains(ex.Message, "[custom].elkt_lookup");
        StringAssert.Contains(ex.Message, reason);
        Assert.AreEqual(0, entity.UpdateCount);
        Assert.IsFalse(messages.Any(message => message.Contains("Skipping lookup assertion")));
    }

    [TestMethod]
    public async Task FullTest_MetadataFailureIncludesProcedureAndOriginalException()
    {
        SetupLookup(null);
        var failure = new InvalidOperationException("Metadata query failed");
        client.Setup(c => c.ExecuteSQLSingleColumn<string>(DefinitionQuery, CancellationToken.None, null)).ThrowsAsync(failure);
        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => EntityTests.FullTest(entity, folder, false, CancellationToken.None, skip_empty_lookups: true));
        StringAssert.Contains(ex.Message, "[custom].elkt_lookup");
        StringAssert.Contains(ex.Message, entity.Def.Name);
        Assert.AreSame(failure, ex.InnerException);
        Assert.AreEqual(0, entity.UpdateCount);
    }

    [TestMethod]
    public async Task FullTest_MetadataCancellationPropagates()
    {
        SetupLookup(null);
        using var cts = new CancellationTokenSource();
        var cancellation = new OperationCanceledException(cts.Token);
        client.Setup(c => c.ExecuteSQLSingleColumn<string>(DefinitionQuery, cts.Token, null))
            .Returns<string, CancellationToken, IEnumerable<ColumnBase>?>((_, _, _) =>
            {
                cts.Cancel();
                return Task.FromException<string?>(cancellation);
            });

        var ex = await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => EntityTests.FullTest(entity, folder, false, cts.Token, skip_empty_lookups: true));
        Assert.AreSame(cancellation, ex);
        Assert.AreEqual(0, entity.UpdateCount);
    }

    [TestMethod]
    public async Task FullTest_LookupExceptionIsNeverSkipped()
    {
        var failure = new InvalidOperationException("Lookup failed");
        client.Setup(c => c.ExecuteSPSingleColumn<string>(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IEnumerable<ColumnBase>?>())).ThrowsAsync(failure);
        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => EntityTests.FullTest(entity, folder, false, CancellationToken.None, skip_empty_lookups: true));
        Assert.AreSame(failure, ex);
        client.VerifyNoOtherCallsExceptLookup();
    }

    [TestMethod]
    public async Task GenericFullTest_ForwardsOptionAndLogging()
    {
        SetupLookup(null);
        SetupDefinition(EmptyDefinition);
        await EntityTests.FullTest<EmptyLookupTestEntity>(client.Object, null!, "unused", "custom", folder, false, CancellationToken.None, skip_empty_lookups: true, log: messages.Add);
        Assert.IsTrue(messages.Any(message => message.Contains("Skipping lookup assertion")));
        client.Verify(c => c.ExecuteSQLSingleColumn<string>(DefinitionQuery, CancellationToken.None, null), Times.Once);
    }

    [TestMethod]
    public async Task GenericFullTest_DefaultStillFailsEmptyLookup()
    {
        SetupLookup(null);
        await Assert.ThrowsExactlyAsync<Exception>(() => EntityTests.FullTest<EmptyLookupTestEntity>(client.Object, null!, "unused", "custom", folder, false, CancellationToken.None));
        client.VerifyNoOtherCallsExceptLookup();
    }

    [TestMethod]
    public async Task Inspect_UsesExplicitProcedureQualifiedName()
    {
        // EntityData uses a registered procedure's schema rather than reconstructing the name.
        var registered = new RegisteredLookupTestEntity();
        registered.Def.elkt_lookup.Schema = "procedure_schema";
        registered.Init(client.Object, schema_name: "entity_schema");
        const string query = "SELECT OBJECT_DEFINITION(OBJECT_ID('[procedure_schema].elkt_lookup', N'P'))";
        SetupDefinition(EmptyDefinition, query);

        var result = await LookupProcedureInspection.Inspect(registered, CancellationToken.None);

        Assert.AreEqual("[procedure_schema].elkt_lookup", result.ProcedureName);
        Assert.AreEqual(LookupProcedureBody.Empty, result.Body);
        client.Verify(c => c.ExecuteSQLSingleColumn<string>(query, CancellationToken.None, null), Times.Once);
        client.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow("custom", "[custom].elkt_lookup", DefinitionQuery)]
    [DataRow(null, "elkt_lookup", "SELECT OBJECT_DEFINITION(OBJECT_ID('elkt_lookup', N'P'))")]
    public async Task Inspect_UsesResolvedProcedureNameInSqlText(string? schema, string expectedName, string expectedQuery)
    {
        var inspected = new EmptyLookupTestEntity();
        inspected.Init(client.Object, schema_name: schema);
        SetupDefinition(EmptyDefinition, expectedQuery);
        using var cts = new CancellationTokenSource();

        var result = await LookupProcedureInspection.Inspect(inspected, cts.Token);

        Assert.AreEqual(expectedName, result.ProcedureName);
        Assert.AreEqual(LookupProcedureBody.Empty, result.Body);
        client.Verify(c => c.ExecuteSQLSingleColumn<string>(expectedQuery, cts.Token, null), Times.Once);
        client.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void GetQualifiedName_PreservesUnqualifiedLookupResolution()
    {
        Assert.AreEqual("elkt_lookup", LookupProcedureInspection.GetQualifiedName(new EmptyLookupTestEntity()));
    }

    [TestMethod]
    public void TestEntryPoints_AcceptExistingAndNewCallPatterns()
    {
        // Compile the public call patterns without opening the batch runner's real connection.
        List<IEntityType> entities = [];
        Func<Task>[] calls =
        [
            () => EntityTests.FullTest(entity, folder, false, CancellationToken.None, messages.Add),
            () => EntityTests.FullTest(entity, folder, false, CancellationToken.None, skip_empty_lookups: true),
            () => EntityTests.FullTest<EmptyLookupTestEntity>(client.Object, null!, "", "custom", folder, false, CancellationToken.None),
            () => entities.RunTestsOnEntities("server", "db", "user", "password", null!, "custom", folder, false, CancellationToken.None, messages.Add),
            () => entities.RunTestsOnEntities("server", "db", "user", "password", null!, "custom", folder, false, CancellationToken.None, log: messages.Add, skip_empty_lookups: true)
        ];
        Assert.HasCount(5, calls);
    }

    private void SetupLookup(string? result) => client
        .Setup(c => c.ExecuteSPSingleColumn<string>("[custom].elkt_lookup", It.IsAny<CancellationToken>(), It.IsAny<IEnumerable<ColumnBase>?>()))
        .ReturnsAsync(result);

    // SQL-text scalar execution does not bind command parameters in DatabaseClient.
    // Matching both the complete query and null parameters prevents the former placeholder query
    // from passing through a mock even though it fails with the real client.
    private void SetupDefinition(string? definition, string query = DefinitionQuery) => client
        .Setup(c => c.ExecuteSQLSingleColumn<string>(query, It.IsAny<CancellationToken>(), null))
        .ReturnsAsync(definition);

    private void WriteFixture(int recordCount)
    {
        var fixture = new
        {
            headers = new[] { "c_lookup_id", "vc_description" },
            records = Enumerable.Range(1, recordCount).Select(id => new[] { id.ToString(), "Description" }).ToArray()
        };
        File.WriteAllText(entity.GetTestDataFilePath(folder), JsonSerializer.Serialize(fixture));
    }
}

internal static class LookupClientVerification
{
    internal static void VerifyNoOtherCallsExceptLookup(this Mock<IEntityClient> client)
    {
        client.Verify(c => c.ExecuteSPSingleColumn<string>(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IEnumerable<ColumnBase>?>()), Times.Once);
        client.VerifyNoOtherCalls();
    }
}

public class EmptyLookupTestDef : EntityDefinition
{
    public EmptyLookupTestDef() : base("elkt", nameof(EmptyLookupTestEntity)) { }
    public readonly Column<string> c_lookup_id = Column<string>.PK();
    public readonly Column<string> vc_description = Column<string>.Text(size: 100);
}

public class EmptyLookupTestEntity : Entity<EmptyLookupTestDef>
{
    private bool deleted;
    public bool AdvanceLastUpdateOnUpdate { get; set; } = true;
    public int InsertCount { get; private set; }
    public int UpdateCount { get; private set; }
    public int DeleteCount { get; private set; }
    public int GetCount { get; private set; }

    public override Task<DBStatusResult> InsertData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        InsertCount++;
        deleted = false;
        Def.dt_lu.Value = new DateTime(2026, 10, 21);
        return Task.FromResult(DBStatusResult.SuccessStatus());
    }

    public override Task<bool> GetData(CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        GetCount++;
        return Task.FromResult(!deleted);
    }

    public override Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        UpdateCount++;
        if (AdvanceLastUpdateOnUpdate) Def.dt_lu.Value = Def.dt_lu.Value.AddSeconds(1);
        return Task.FromResult(DBStatusResult.SuccessStatus());
    }

    public override Task<DBStatusResult> DeleteData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        DeleteCount++;
        deleted = true;
        return Task.FromResult(DBStatusResult.SuccessStatus());
    }
}

public sealed class RegisteredLookupTestDef : EntityDefinition
{
    public RegisteredLookupTestDef() : base("elkt", nameof(RegisteredLookupTestEntity)) { }
    public readonly Column<string> c_lookup_id = Column<string>.PK();
    public readonly ProcedureDefinition elkt_lookup = new(nameof(c_lookup_id));
}

public sealed class RegisteredLookupTestEntity : Entity<RegisteredLookupTestDef> { }
