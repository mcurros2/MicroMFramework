using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Excel;
using MicroM.ImportData;
using MicroM.Web.Controllers;
using MicroM.Web.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sylvan.Data.Excel;

namespace LibraryTest;

[TestClass]
public class ExcelImportTests
{
    [TestMethod]
    public async Task ReadExcelAsync_StreamsRowsAndLeavesInputOpen()
    {
        using var stream = await CreateWorkbook();
        var rows = ExcelReader.ReadExcelAsync(stream, "Import");
        await using var enumerator = rows.GetAsyncEnumerator();

        Assert.IsTrue(await enumerator.MoveNextAsync());
        CollectionAssert.AreEqual(new object?[] { "Source ID", "Description" }, enumerator.Current);

        await enumerator.DisposeAsync();
        Assert.IsTrue(stream.CanRead);
    }

    [TestMethod]
    public async Task ReadExcelAsync_UsesConfiguredHeaderRow()
    {
        using var stream = await CreateWorkbook();
        List<object?[]> rows = [];

        await foreach (var row in ExcelReader.ReadExcelAsync(stream, "Import", initialRow: 2))
        {
            rows.Add(row);
        }

        Assert.HasCount(2, rows);
        CollectionAssert.AreEqual(new object?[] { "Q1", "First" }, rows[0]);
        CollectionAssert.AreEqual(new object?[] { "Q2", "Second" }, rows[1]);
    }

    [TestMethod]
    public async Task ReadExcelAsync_ThrowsForMissingWorksheet()
    {
        using var stream = await CreateWorkbook();

        await Assert.ThrowsExactlyAsync<InvalidDataException>(async () =>
        {
            await foreach (var _ in ExcelReader.ReadExcelAsync(stream, "Missing"))
            {
            }
        });
    }

    [TestMethod]
    public async Task ReadExcelAsync_HonorsCancellation()
    {
        using var stream = await CreateWorkbook();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in ExcelReader.ReadExcelAsync(stream, "Import", ct: cts.Token))
            {
            }
        });
    }

    [TestMethod]
    public void ResolveImportMapping_UsesOptionalIndexAndHeaderDisambiguation()
    {
        TestQueue entity = new();
        object?[] headers = ["Fallback", "Description", "Description"];
        FileImportMapping mapping = new()
        {
            Mapping =
            [
                new(null, 0, entity.Def.c_queue_id.Name),
                new("description", 2, entity.Def.vc_description.Name),
                new("Fallback", 99, entity.Def.dt_init.Name)
            ]
        };

        var resolved = EntityImportData.ResolveImportMapping(entity, headers, mapping);

        Assert.HasCount(3, resolved);
        Assert.AreEqual(0, resolved[0].SourceIndex);
        Assert.AreEqual(2, resolved[1].SourceIndex);
        Assert.AreEqual(0, resolved[2].SourceIndex);
    }

    [TestMethod]
    public void ResolveImportMapping_RejectsMissingHeaderEvenWhenIndexIsPresent()
    {
        TestQueue entity = new();

        Assert.ThrowsExactly<InvalidDataException>(() => EntityImportData.ResolveImportMapping(
            entity,
            ["Source ID", "Description"],
            new FileImportMapping
            {
                Mapping = [new("Missing", 0, entity.Def.c_queue_id.Name)]
            }));
    }

    [TestMethod]
    public void ResolveImportMapping_RejectsInvalidConfiguration()
    {
        TestQueue entity = new();
        object?[] headers = ["Source ID", "Description"];

        Assert.ThrowsExactly<InvalidDataException>(() => EntityImportData.ResolveImportMapping(
            entity,
            headers,
            new FileImportMapping
            {
                Mapping =
                [
                    new("Source ID", 0, entity.Def.c_queue_id.Name),
                    new("Description", 1, entity.Def.c_queue_id.Name)
                ]
            }));

        Assert.ThrowsExactly<InvalidDataException>(() => EntityImportData.ResolveImportMapping(
            entity,
            headers,
            new FileImportMapping
            {
                Mapping = [new(null, 10, entity.Def.vc_description.Name)]
            }));

        Assert.ThrowsExactly<InvalidDataException>(() => EntityImportData.ResolveImportMapping(
            entity,
            headers,
            new FileImportMapping
            {
                Mapping = [new("Source ID", 0, "does_not_exist")]
            }));

        Assert.ThrowsExactly<InvalidDataException>(() => EntityImportData.ResolveImportMapping(
            entity,
            headers,
            new FileImportMapping
            {
                Mapping = [new(null, null, entity.Def.vc_description.Name)]
            }));

        Assert.ThrowsExactly<InvalidDataException>(() => EntityImportData.ResolveImportMapping(
            entity,
            ["Description", "Description"],
            new FileImportMapping
            {
                Mapping = [new("Description", null, entity.Def.vc_description.Name)]
            }));
    }

    [TestMethod]
    public void ImportDataWebAPIRequest_DeserializesLegacyAndMappedBodies()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var legacy = JsonSerializer.Deserialize<ImportDataWebAPIRequest>("""
            { "values": {}, "parentKeys": {}, "recordsSelection": [] }
            """, options);

        Assert.IsNotNull(legacy);
        Assert.IsNull(legacy.FileImportMapping);

        var mapped = JsonSerializer.Deserialize<ImportDataWebAPIRequest>("""
            {
              "values": {},
              "fileImportMapping": {
                "sheetName": "Import",
                "mapping": [
                  { "sourceHeader": "Amount", "destinationColumnName": "n_amount" },
                  { "sourceIndex": 2, "destinationColumnName": "i_count" }
                ]
              }
            }
            """, options);

        Assert.IsNotNull(mapped?.FileImportMapping);
        Assert.AreEqual("Import", mapped.FileImportMapping.SheetName);
        Assert.HasCount(2, mapped.FileImportMapping.Mapping);
        Assert.IsNull(mapped.FileImportMapping.Mapping[0].SourceIndex);
        Assert.IsNull(mapped.FileImportMapping.Mapping[1].SourceHeader);
        Assert.AreEqual(2, mapped.FileImportMapping.Mapping[1].SourceIndex);
    }

    [TestMethod]
    public void ImportEndpointContracts_UseImportSpecificRequest()
    {
        AssertImportRequestParameter(typeof(IEntitiesController), nameof(IEntitiesController.Import));
        AssertImportRequestParameter(typeof(EntitiesController), nameof(EntitiesController.Import));
        AssertImportRequestParameter(typeof(IEntitiesService), nameof(IEntitiesService.HandleImportData));
        AssertImportRequestParameter(typeof(EntitiesService), nameof(EntitiesService.HandleImportData));
    }

    [TestMethod]
    public void ParseCSVTable_UsesConfiguredHeaderRowAndQuotedValues()
    {
        CSVTable table = CSVParser.ParseTable(
            "Report title\r\nSource ID,Description\r\nQ1,\"First, quoted\"\r\nQ2,Second\r\n",
            initialRow: 2,
            CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "Source ID", "Description" }, table.Headers);
        Assert.HasCount(2, table.Rows);
        CollectionAssert.AreEqual(new[] { "Q1", "First, quoted" }, table.Rows[0]);
        CollectionAssert.AreEqual(new[] { "Q2", "Second" }, table.Rows[1]);
    }

    [TestMethod]
    public async Task ImportDataFromCSV_AppliesExplicitMappingAndNullsOmittedDestinations()
    {
        CSVTable table = new(
            ["Source Amount", "Extra"],
            [["1234.56", "ignored"]]);
        FileImportMapping mapping = new()
        {
            Mapping = [new("Source Amount", null, nameof(TypedExcelImportDef.n_value))]
        };
        var (entity, api, entityData) = CreateTypedImportEntity();
        entity.Def.i_value.ValueObject = 42;
        decimal? insertedAmount = null;
        object? insertedOmittedValue = 42;
        entityData
            .Setup(data => data.InsertData(It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Callback(() =>
            {
                insertedAmount = entity.Def.n_value.ValueObject as decimal?;
                insertedOmittedValue = entity.Def.i_value.ValueObject;
            })
            .ReturnsAsync(DBStatusResult.SuccessStatus());

        var result = await entity.ImportDataFromCSV(
            table,
            mapping,
            new MicroMOptions(),
            claims: null,
            api.Object,
            app_id: "test",
            parentKeys: null,
            CancellationToken.None);

        Assert.AreEqual(1, result.SuccessCount);
        Assert.AreEqual(1234.56m, insertedAmount);
        Assert.IsNull(insertedOmittedValue);
    }

    [TestMethod]
    public async Task ImportDataFromExcel_MapsTypedCellsToDestinationColumnTypes()
    {
        DateTime expectedDate = new(2026, 8, 24, 14, 30, 15, DateTimeKind.Unspecified);
        object?[] sourceValues = [expectedDate, 1234.56m, 42, true, 12.75d];
        using var stream = await CreateTypedWorkbook(sourceValues);
        var (entity, api, entityData) = CreateTypedImportEntity();
        List<object?[]> insertedValues = [];
        entityData
            .Setup(data => data.InsertData(It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Callback(() => insertedValues.Add(GetTypedColumnValues(entity)))
            .ReturnsAsync(DBStatusResult.SuccessStatus());

        var result = await entity.ImportDataFromExcel(
            stream,
            ExcelWorkbookType.ExcelXml,
            CreateTypedMapping(),
            initialRow: null,
            new MicroMOptions(),
            claims: null,
            api.Object,
            app_id: "test",
            parentKeys: null,
            CancellationToken.None);

        Assert.AreEqual(1, result.SuccessCount);
        Assert.AreEqual(0, result.ErrorCount);
        Assert.HasCount(1, insertedValues);
        Assert.AreEqual(expectedDate, insertedValues[0][0]);
        Assert.AreEqual(1234.56m, insertedValues[0][1]);
        Assert.AreEqual(42, insertedValues[0][2]);
        Assert.IsTrue((bool)insertedValues[0][3]!);
        Assert.AreEqual(12.75d, insertedValues[0][4]);
    }

    [TestMethod]
    public async Task ImportDataFromExcel_MapsInvariantStringsToDestinationColumnTypes()
    {
        object?[] sourceValues = ["2026-08-24T14:30:15", "1234.56", "42", "true", "12.75"];
        using var stream = await CreateTypedWorkbook(sourceValues);
        var (entity, api, entityData) = CreateTypedImportEntity();
        List<object?[]> insertedValues = [];
        entityData
            .Setup(data => data.InsertData(It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Callback(() => insertedValues.Add(GetTypedColumnValues(entity)))
            .ReturnsAsync(DBStatusResult.SuccessStatus());

        var result = await entity.ImportDataFromExcel(
            stream,
            ExcelWorkbookType.ExcelXml,
            CreateTypedMapping(),
            initialRow: null,
            new MicroMOptions(),
            claims: null,
            api.Object,
            app_id: "test",
            parentKeys: null,
            CancellationToken.None);

        Assert.AreEqual(1, result.SuccessCount);
        Assert.HasCount(1, insertedValues);
        Assert.AreEqual(new DateTime(2026, 8, 24, 14, 30, 15), insertedValues[0][0]);
        Assert.AreEqual(1234.56m, insertedValues[0][1]);
        Assert.AreEqual(42, insertedValues[0][2]);
        Assert.IsTrue((bool)insertedValues[0][3]!);
        Assert.AreEqual(12.75d, insertedValues[0][4]);
    }

    [TestMethod]
    [DataRow(42.5d)]
    [DataRow(2147483648d)]
    public async Task ImportDataFromExcel_RejectsInvalidIntWithoutInserting(double invalidInt)
    {
        object?[] sourceValues = [new DateTime(2026, 8, 24), 1m, invalidInt, true, 1d];
        using var stream = await CreateTypedWorkbook(sourceValues);
        var (entity, api, entityData) = CreateTypedImportEntity();
        entityData
            .Setup(data => data.InsertData(It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(DBStatusResult.SuccessStatus());

        var result = await entity.ImportDataFromExcel(
            stream,
            ExcelWorkbookType.ExcelXml,
            CreateTypedMapping(),
            initialRow: null,
            new MicroMOptions(),
            claims: null,
            api.Object,
            app_id: "test",
            parentKeys: null,
            CancellationToken.None);

        Assert.AreEqual(0, result.SuccessCount);
        Assert.AreEqual(1, result.ErrorCount);
        entityData.Verify(data => data.InsertData(It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public void ConvertExcelValue_ConvertsSupportedScalarTypes()
    {
        TypedExcelImportEntity entity = new();
        Guid expectedGuid = Guid.Parse("5f3f3391-e579-4736-a6e0-ec5fd982645c");

        Assert.AreEqual(9223372036854775806L, EntityImportData.ConvertExcelValue(entity.Def.bi_value, "9223372036854775806"));
        Assert.AreEqual(32767, EntityImportData.ConvertExcelValue(entity.Def.i_value, 32767m));
        Assert.AreEqual((short)-123, EntityImportData.ConvertExcelValue(entity.Def.si_value, "-123"));
        Assert.AreEqual((byte)255, EntityImportData.ConvertExcelValue(entity.Def.ti_value, "255"));
        Assert.AreEqual(1234.5m, EntityImportData.ConvertExcelValue(entity.Def.n_value, "1,234.5"));
        Assert.AreEqual(12.75d, EntityImportData.ConvertExcelValue(entity.Def.f_value, "12.75"));
        Assert.AreEqual(12.75f, EntityImportData.ConvertExcelValue(entity.Def.r_value, 12.75m));
        Assert.IsTrue((bool)EntityImportData.ConvertExcelValue(entity.Def.bt_value, "1")!);
        Assert.AreEqual(expectedGuid, EntityImportData.ConvertExcelValue(entity.Def.ui_value, expectedGuid.ToString("B")));
    }

    [TestMethod]
    [DoNotParallelize]
    public void ConvertExcelValue_UsesInvariantCultureForNumbersAndText()
    {
        TypedExcelImportEntity entity = new();
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-AR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-AR");

            Assert.AreEqual(1234.5m, EntityImportData.ConvertExcelValue(entity.Def.n_value, "1234.5"));
            Assert.AreEqual("1234.5", EntityImportData.ConvertExcelValue(entity.Def.vc_text, 1234.5m));
            Assert.AreEqual("12.75", EntityImportData.ConvertExcelValue(entity.Def.vc_text, 12.75d));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [TestMethod]
    [DataRow("20260824")]
    [DataRow("2026-08-24")]
    [DataRow("2026/08/24")]
    [DataRow("2026.08.24")]
    public void ConvertExcelValue_AcceptsUnambiguousYearFirstDates(string sourceValue)
    {
        TypedExcelImportEntity entity = new();

        Assert.AreEqual(
            new DateTime(2026, 8, 24),
            EntityImportData.ConvertExcelValue(entity.Def.dt_value, sourceValue));
        Assert.AreEqual(
            new DateOnly(2026, 8, 24),
            EntityImportData.ConvertExcelValue(entity.Def.d_value, sourceValue));
    }

    [TestMethod]
    public void ConvertExcelValue_ConvertsDateTimesTimesAndOffsets()
    {
        TypedExcelImportEntity entity = new();

        Assert.AreEqual(
            new DateTime(2026, 8, 24, 14, 30, 15, 123, DateTimeKind.Unspecified).AddTicks(4560),
            EntityImportData.ConvertExcelValue(entity.Def.dt_value, "2026-08-24T14:30:15.123456"));

        var utc = (DateTime)EntityImportData.ConvertExcelValue(entity.Def.dt_value, "2026-08-24T14:30:15Z")!;
        Assert.AreEqual(DateTimeKind.Utc, utc.Kind);
        Assert.AreEqual(new DateTime(2026, 8, 24, 14, 30, 15, DateTimeKind.Utc), utc);

        Assert.AreEqual(new TimeOnly(14, 30, 15), EntityImportData.ConvertExcelValue(entity.Def.t_value, "14:30:15"));
        Assert.AreEqual(new TimeOnly(12, 0), EntityImportData.ConvertExcelValue(entity.Def.t_value, 0.5d));

        var expectedOffset = new DateTimeOffset(2026, 8, 24, 14, 30, 15, TimeSpan.FromHours(-3));
        Assert.AreEqual(expectedOffset, EntityImportData.ConvertExcelValue(entity.Def.dto_value, "2026-08-24T14:30:15-03:00"));
    }

    [TestMethod]
    public void ConvertExcelValue_FormatsScalarValuesAsInvariantText()
    {
        TypedExcelImportEntity entity = new();
        DateTime dateTime = new(2026, 8, 24, 14, 30, 15, DateTimeKind.Utc);
        DateTimeOffset dateTimeOffset = new(2026, 8, 24, 14, 30, 15, TimeSpan.FromHours(-3));
        Guid guid = Guid.Parse("5f3f3391-e579-4736-a6e0-ec5fd982645c");

        Assert.AreEqual("2026-08-24", EntityImportData.ConvertExcelValue(entity.Def.vc_text, new DateOnly(2026, 8, 24)));
        Assert.AreEqual("2026-08-24T14:30:15.0000000Z", EntityImportData.ConvertExcelValue(entity.Def.vc_text, dateTime));
        Assert.AreEqual("2026-08-24T14:30:15.0000000-03:00", EntityImportData.ConvertExcelValue(entity.Def.vc_text, dateTimeOffset));
        Assert.AreEqual("14:30:15.0000000", EntityImportData.ConvertExcelValue(entity.Def.vc_text, new TimeOnly(14, 30, 15)));
        Assert.AreEqual("True", EntityImportData.ConvertExcelValue(entity.Def.vc_text, true));
        Assert.AreEqual(guid.ToString("D"), EntityImportData.ConvertExcelValue(entity.Def.vc_text, guid));
    }

    [TestMethod]
    public void ConvertExcelValue_RejectsAmbiguousOrUnsafeConversions()
    {
        TypedExcelImportEntity entity = new();

        Assert.ThrowsExactly<FormatException>(() => EntityImportData.ConvertExcelValue(entity.Def.dt_value, "08/09/2026"));
        Assert.ThrowsExactly<FormatException>(() => EntityImportData.ConvertExcelValue(entity.Def.dto_value, "2026-08-24T14:30:15"));
        Assert.ThrowsExactly<FormatException>(() => EntityImportData.ConvertExcelValue(entity.Def.i_value, 42.5d));
        Assert.ThrowsExactly<FormatException>(() => EntityImportData.ConvertExcelValue(entity.Def.si_value, short.MaxValue + 1));
        Assert.ThrowsExactly<FormatException>(() => EntityImportData.ConvertExcelValue(entity.Def.ti_value, -1));
        Assert.ThrowsExactly<FormatException>(() => EntityImportData.ConvertExcelValue(entity.Def.f_value, double.PositiveInfinity));
        Assert.ThrowsExactly<FormatException>(() => EntityImportData.ConvertExcelValue(entity.Def.r_value, double.MaxValue));
        Assert.ThrowsExactly<FormatException>(() => EntityImportData.ConvertExcelValue(entity.Def.vb_value, "not implicitly encoded"));
        Assert.ThrowsExactly<FormatException>(() => EntityImportData.ConvertExcelValue(entity.Def.x_value, 123));
    }

    [TestMethod]
    public async Task ReadExcelAsync_LargeWorkbookCanStopAfterHeader()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "1mb.xlsx");
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var enumerator = ExcelReader.ReadExcelAsync(stream, null).GetAsyncEnumerator();

        Assert.IsTrue(await enumerator.MoveNextAsync());
        Assert.IsGreaterThan(0, enumerator.Current.Length);
    }

    private static async Task<MemoryStream> CreateWorkbook()
    {
        DataResult data = new(["Source ID", "Description"], ["String", "String"]);
        data.records.Add(["Q1", "First"]);
        data.records.Add(["Q2", "Second"]);

        MemoryStream stream = new();
        await data.SaveAsExcelToStreamAsync(stream, "Import", use_inline_strings: true);
        stream.Position = 0;
        return stream;
    }

    private static async Task<MemoryStream> CreateTypedWorkbook(object?[] values)
    {
        DataResult data = new(
            ["Date", "Amount", "Count", "Enabled", "Ratio"],
            ["DateTime", "Decimal", "Int32", "Boolean", "Double"]);
        data.records.Add(values);

        MemoryStream stream = new();
        await data.SaveAsExcelToStreamAsync(stream, "Import", use_inline_strings: true);
        stream.Position = 0;
        return stream;
    }

    private static FileImportMapping CreateTypedMapping()
    {
        return new FileImportMapping
        {
            SheetName = "Import",
            Mapping =
            [
                new("Date", null, nameof(TypedExcelImportDef.dt_value)),
                new("Amount", null, nameof(TypedExcelImportDef.n_value)),
                new("Count", null, nameof(TypedExcelImportDef.i_value)),
                new("Enabled", null, nameof(TypedExcelImportDef.bt_value)),
                new("Ratio", null, nameof(TypedExcelImportDef.f_value))
            ]
        };
    }

    private static (TypedExcelImportEntity entity, Mock<IWebAPIServices> api, Mock<IEntityData> entityData) CreateTypedImportEntity()
    {
        Mock<IEntityClient> client = new();
        client
            .Setup(item => item.Connect(It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(true);
        client.Setup(item => item.Disconnect()).Returns(Task.CompletedTask);

        Mock<IEntityData> entityData = new();
        entityData.SetupGet(item => item.EntityClient).Returns(client.Object);

        TypedExcelImportEntity entity = new();
        entity.Init(client.Object, data: entityData.Object);

        Mock<IEntitiesService> entitiesService = new();
        entitiesService.Setup(item => item.GetApplicationKeys("test")).Returns([]);

        Mock<IWebAPIServices> api = new();
        api.SetupGet(item => item.entitiesService).Returns(entitiesService.Object);
        return (entity, api, entityData);
    }

    private static object?[] GetTypedColumnValues(TypedExcelImportEntity entity)
    {
        return
        [
            entity.Def.dt_value.ValueObject,
            entity.Def.n_value.ValueObject,
            entity.Def.i_value.ValueObject,
            entity.Def.bt_value.ValueObject,
            entity.Def.f_value.ValueObject
        ];
    }

    private static void AssertImportRequestParameter(Type contractType, string methodName)
    {
        var method = contractType.GetMethod(methodName);
        Assert.IsNotNull(method);
        var parameter = Array.Find(method.GetParameters(), item => item.Name == "parms");
        Assert.IsNotNull(parameter);
        Assert.AreEqual(typeof(ImportDataWebAPIRequest), parameter.ParameterType);
    }
}

public sealed class TypedExcelImportDef : EntityDefinition
{
    public TypedExcelImportDef() : base("exit", nameof(TypedExcelImportEntity), add_default_columns: false)
    {
    }

    public readonly Column<DateTime> dt_value = new(sql_type: SqlDbType.DateTime2);
    public readonly Column<decimal> n_value = new(sql_type: SqlDbType.Decimal, precision: 18, scale: 2);
    public readonly Column<int> i_value = new(sql_type: SqlDbType.Int);
    public readonly Column<bool> bt_value = new(sql_type: SqlDbType.Bit);
    public readonly Column<double> f_value = new(sql_type: SqlDbType.Float);
    public readonly Column<long> bi_value = new(sql_type: SqlDbType.BigInt);
    public readonly Column<short> si_value = new(sql_type: SqlDbType.SmallInt);
    public readonly Column<byte> ti_value = new(sql_type: SqlDbType.TinyInt);
    public readonly Column<float> r_value = new(sql_type: SqlDbType.Real);
    public readonly Column<string> vc_text = new(sql_type: SqlDbType.VarChar);
    public readonly Column<DateOnly> d_value = new(sql_type: SqlDbType.Date);
    public readonly Column<TimeOnly> t_value = new(sql_type: SqlDbType.Time);
    public readonly Column<DateTimeOffset> dto_value = new(sql_type: SqlDbType.DateTimeOffset);
    public readonly Column<Guid> ui_value = new(sql_type: SqlDbType.UniqueIdentifier);
    public readonly Column<byte[]> vb_value = new(sql_type: SqlDbType.VarBinary);
    public readonly Column<string> x_value = new(sql_type: SqlDbType.Xml);
}

public sealed class TypedExcelImportEntity : Entity<TypedExcelImportDef>
{
}
