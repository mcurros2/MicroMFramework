using MicroM.Data;
using MicroM.Excel;
using MicroM.ImportData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
    public void ResolveExcelMapping_UsesHeaderFirstThenIndexFallback()
    {
        TestQueue entity = new();
        object?[] headers = ["Fallback", "Description", "Description"];
        ExcelImportMapping mapping = new()
        {
            Mapping =
            [
                new("Missing", 0, entity.Def.c_queue_id.Name),
                new("description", 2, entity.Def.vc_description.Name),
                new("Fallback", 99, entity.Def.dt_init.Name)
            ]
        };

        var resolved = EntityImportData.ResolveExcelMapping(entity, headers, mapping);

        Assert.HasCount(3, resolved);
        Assert.AreEqual(0, resolved[0].SourceIndex);
        Assert.AreEqual(2, resolved[1].SourceIndex);
        Assert.AreEqual(0, resolved[2].SourceIndex);
    }

    [TestMethod]
    public void ResolveExcelMapping_RejectsInvalidConfiguration()
    {
        TestQueue entity = new();
        object?[] headers = ["Source ID", "Description"];

        Assert.ThrowsExactly<InvalidDataException>(() => EntityImportData.ResolveExcelMapping(
            entity,
            headers,
            new ExcelImportMapping
            {
                Mapping =
                [
                    new("Source ID", 0, entity.Def.c_queue_id.Name),
                    new("Description", 1, entity.Def.c_queue_id.Name)
                ]
            }));

        Assert.ThrowsExactly<InvalidDataException>(() => EntityImportData.ResolveExcelMapping(
            entity,
            headers,
            new ExcelImportMapping
            {
                Mapping = [new("Missing", 10, entity.Def.vc_description.Name)]
            }));

        Assert.ThrowsExactly<InvalidDataException>(() => EntityImportData.ResolveExcelMapping(
            entity,
            headers,
            new ExcelImportMapping
            {
                Mapping = [new("Source ID", 0, "does_not_exist")]
            }));
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
}
