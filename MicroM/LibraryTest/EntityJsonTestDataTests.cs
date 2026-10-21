using MicroM.AutomatedTests;
using MicroM.Core;
using MicroM.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LibraryTest;

[TestClass]
public class EntityJsonTestDataTests
{
    [TestMethod]
    [DataRow("-32768", (short)-32768)]
    [DataRow("0", (short)0)]
    [DataRow("32767", (short)32767)]
    public void ApplyRecord_LoadsShortWithExactClrType(string json, short expected)
    {
        var column = new Column<short>("si_value");
        Apply(column, json);
        Assert.IsInstanceOfType<short>(column.ValueObject);
        Assert.AreEqual(expected, column.Value);

        var nullable = new Column<short?>("si_value");
        Apply(nullable, json);
        Assert.AreEqual((short?)expected, nullable.Value);
    }

    [TestMethod]
    [DataRow("\"2026-10-21T12:34:56Z\"")]
    [DataRow("\"2026-10-21T12:34:56.1234567-03:00\"")]
    [DataRow("\"2026-10-21T12:34:56.1234567+05:30\"")]
    public void ApplyRecord_LoadsDateTimeOffsetAndPreservesOffset(string json)
    {
        var expected = DateTimeOffset.Parse(JsonSerializer.Deserialize<string>(json)!, CultureInfo.InvariantCulture);
        var column = new Column<DateTimeOffset>("dto_value");
        Apply(column, json);
        Assert.IsInstanceOfType<DateTimeOffset>(column.ValueObject);
        Assert.IsTrue(expected.EqualsExact(column.Value));

        var nullable = new Column<DateTimeOffset?>("dto_value");
        Apply(nullable, json);
        Assert.IsTrue(expected.EqualsExact(nullable.Value!.Value));
    }

    [TestMethod]
    [DataRow(SqlDbType.Binary)]
    [DataRow(SqlDbType.VarBinary)]
    [DataRow(SqlDbType.Image)]
    [DataRow(SqlDbType.Timestamp)]
    public void ApplyRecord_LoadsAllBinaryEncodingsAndByteValues(SqlDbType sqlType)
    {
        byte[] expected = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        string[] encodings =
        [
            JsonSerializer.Serialize(Convert.ToBase64String(expected)),
            "[" + string.Join(",", expected) + "]",
            JsonSerializer.Serialize("0x" + Convert.ToHexString(expected)),
            JsonSerializer.Serialize("0X" + Convert.ToHexString(expected).ToLowerInvariant())
        ];

        var column = new Column<byte[]>("vb_value", sql_type: sqlType);
        foreach (var json in encodings)
        {
            Apply(column, json);
            Assert.IsInstanceOfType<byte[]>(column.ValueObject);
            CollectionAssert.AreEqual(expected, column.Value);
        }
    }

    [TestMethod]
    [DataRow("\"\"")]
    [DataRow("[]")]
    [DataRow("\"0x\"")]
    [DataRow("\"0X\"")]
    public void ApplyRecord_LoadsEmptyBinaryValues(string json)
    {
        var column = new Column<byte[]>("vb_value", value: [42]);
        Apply(column, json);
        CollectionAssert.AreEqual(Array.Empty<byte>(), column.Value);
    }

    [TestMethod]
    public void ApplyRecord_PreservesNullAndUndefinedHandling()
    {
        ColumnBase[] columns =
        [
            new Column<short?>("si_value", value: 42),
            new Column<DateTimeOffset?>("dto_value", value: DateTimeOffset.UtcNow),
            new Column<byte[]?>("vb_value", value: [42], nullable: true)
        ];
        foreach (var column in columns)
        {
            var initial = column.ValueObject;
            Apply(column, "null");
            Assert.IsNull(column.ValueObject);

            column.ValueObject = initial;
            var data = new EntityJsonTestData { headers = [column.Name] };
            data.ApplyRecordToColumns([default], new CustomOrderedDictionary<ColumnBase>().Add(column.Name, column));
            Assert.IsNull(column.ValueObject);
        }
    }

    [TestMethod]
    [DataRow("32768")]
    [DataRow("-32769")]
    [DataRow("1.5")]
    [DataRow("\"42\"")]
    [DataRow("true")]
    [DataRow("[]")]
    public void ApplyRecord_RejectsInvalidShortWithoutChangingColumn(string json)
    {
        AssertInvalid(new Column<short>("si_value", value: 42), json);
    }

    [TestMethod]
    [DataRow("\"2026-10-21T12:34:56\"")]
    [DataRow("\"2026-10-21\"")]
    [DataRow("\"2026-02-30T12:34:56Z\"")]
    [DataRow("\"2026-10-21T12:34:56+15:00\"")]
    [DataRow("\"not a date\"")]
    [DataRow("42")]
    [DataRow("{}")]
    public void ApplyRecord_RejectsInvalidDateTimeOffsetWithoutChangingColumn(string json)
    {
        AssertInvalid(new Column<DateTimeOffset>("dto_value", value: DateTimeOffset.UtcNow), json);
    }

    [TestMethod]
    [DataRow("\"not base64!\"")]
    [DataRow("\"0x0\"")]
    [DataRow("\"0xGG\"")]
    [DataRow("\"0xAA BB\"")]
    [DataRow("[0,256]")]
    [DataRow("[-1]")]
    [DataRow("[1.5]")]
    [DataRow("[\"1\"]")]
    [DataRow("[null]")]
    [DataRow("[true]")]
    [DataRow("[[1]]")]
    [DataRow("{}")]
    [DataRow("42")]
    public void ApplyRecord_RejectsInvalidBinaryWithoutChangingColumn(string json)
    {
        AssertInvalid(new Column<byte[]>("vb_value", value: [42]), json);
    }

    [TestMethod]
    public async Task LoadJsonEntityTestData_AppliesMixedFixtureFromFile()
    {
        var entity = new JsonFixtureEntity();
        string path = Path.Combine(Path.GetTempPath(), $"microm-json-fixture-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, """
                {
                  "headers": ["si_value", "dto_value", "vb_value", "vc_value", "i_value", "bt_value", "n_value", "ui_value"],
                  "records": [[-123, "2026-10-21T12:34:56.1234567-03:00", "0x00FF80", "Text with \"quotes\"", 42, true, 123.45, "9b1c104a-4eb2-4f20-9470-15bb95c6f8ac"]]
                }
                """);

            var data = await entity.LoadJsonEntityTestData(path);
            data.ApplyRecordToColumns(data.records.Single(), entity.Def.Columns);

            Assert.AreEqual((short)-123, entity.Def.si_value.Value);
            var expectedOffset = DateTimeOffset.Parse("2026-10-21T12:34:56.1234567-03:00", CultureInfo.InvariantCulture);
            Assert.IsTrue(expectedOffset.EqualsExact(entity.Def.dto_value.Value));
            CollectionAssert.AreEqual(new byte[] { 0, 255, 128 }, entity.Def.vb_value.Value);
            Assert.AreEqual("Text with \"quotes\"", entity.Def.vc_value.Value);
            Assert.AreEqual(42, entity.Def.i_value.Value);
            Assert.IsTrue(entity.Def.bt_value.Value);
            Assert.AreEqual(123.45m, entity.Def.n_value.Value);
            Assert.AreEqual(Guid.Parse("9b1c104a-4eb2-4f20-9470-15bb95c6f8ac"), entity.Def.ui_value.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void Apply(ColumnBase column, string json)
    {
        var data = new EntityJsonTestData { headers = [column.Name] };
        using var document = JsonDocument.Parse(json);
        data.ApplyRecordToColumns([document.RootElement], new CustomOrderedDictionary<ColumnBase>().Add(column.Name, column));
    }

    private static void AssertInvalid(ColumnBase column, string json)
    {
        object? previous = column.ValueObject;
        var ex = Assert.ThrowsExactly<FormatException>(() => Apply(column, json));
        StringAssert.Contains(ex.Message, column.Name);
        StringAssert.Contains(ex.Message, "value position 1");
        StringAssert.Contains(ex.Message, column.SystemType.ToString());
        StringAssert.Contains(ex.Message, column.SQLMetadata.SQLType.ToString());
        Assert.IsNotNull(ex.InnerException);
        Assert.AreSame(previous, column.ValueObject);
    }
}

public sealed class JsonFixtureDef : EntityDefinition
{
    public JsonFixtureDef() : base("jsft", nameof(JsonFixtureEntity), add_default_columns: false) { }

    public readonly Column<short> si_value = new();
    public readonly Column<DateTimeOffset> dto_value = new();
    public readonly Column<byte[]> vb_value = new();
    public readonly Column<string> vc_value = new();
    public readonly Column<int> i_value = new();
    public readonly Column<bool> bt_value = new();
    public readonly Column<decimal> n_value = new(precision: 18, scale: 2);
    public readonly Column<Guid> ui_value = new();
}

public sealed class JsonFixtureEntity : Entity<JsonFixtureDef> { }
