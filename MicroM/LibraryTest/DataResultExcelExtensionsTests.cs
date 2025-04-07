using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MicroM.Data;
using MicroM.Excel;

namespace LibraryTest
{
    [TestClass]
    public class DataResultExcelExtensionsTests
    {
        [TestMethod]
        public async Task SaveAsExcelToStreamAsync_SingleDataResult_CreatesExcelFile()
        {
            var dataResult = new DataResult(new[] { "Header1", "Header2", "Header3", "Header4", "Header5", "", "Empty" }, new[] { "String", "Double", "Long", "DateTime", "GUID", "object", "string" });
            dataResult.records.Add(new object?[] { "Value1", 123.45, 1234567890123456789L, new DateTime(2025, 4, 4), Guid.NewGuid(), null, "" });
            dataResult.records.Add(new object?[] { "Value2", 678.90, 9876543210987654321L, new DateTime(2025, 5, 5), Guid.NewGuid(), null, "" });

            using var memoryStream = new MemoryStream();

            await dataResult.SaveAsExcelToStreamAsync(memoryStream, "Sheet1");

            Assert.IsTrue(memoryStream.Length > 0);

            // Verify the content using ExcelReader
            await memoryStream.FlushAsync();
            memoryStream.Position = 0; // Reset stream position

            // Output the Excel XML to console for debugging
            //using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Read, true))
            //{
            //    Console.WriteLine("----- EXCEL XML CONTENTS -----");
            //    foreach (var entry in archive.Entries)
            //    {
            //        if (entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            //        {
            //            Console.WriteLine($"\n>>> {entry.FullName} <<<");
            //            using var reader = new StreamReader(entry.Open());
            //            var xmlContent = await reader.ReadToEndAsync();
            //            Console.WriteLine(xmlContent);
            //        }
            //    }
            //    Console.WriteLine("----- END OF XML CONTENTS -----");
            //}

            memoryStream.Position = 0; // Reset stream position

            var readData = new List<object?[]>();
            var rows = ExcelReader.ReadExcelAsync(memoryStream, "Sheet1");
            await foreach (var row in rows)
            {
                readData.Add(row);
            }

            // Verify headers
            var expectedHeaders = new[] { "Header1", "Header2", "Header3", "Header4", "Header5", "", "Empty" };
            CollectionAssert.AreEqual(expectedHeaders, readData[0]);

            // Verify data
            for (int i = 0; i < dataResult.records.Count; i++)
            {
                var expectedRow = dataResult.records[i];
                var actualRow = readData[i + 1]; // Skip header row

                Assert.AreEqual(expectedRow[0]?.ToString(), actualRow[0]?.ToString());
                Assert.AreEqual(Convert.ToDouble(expectedRow[1]), Convert.ToDouble(actualRow[1]));
                Assert.AreEqual(Convert.ToDouble(expectedRow[2]), Convert.ToDouble(actualRow[2]));
                Assert.AreEqual(Convert.ToDateTime(expectedRow[3]), Convert.ToDateTime(actualRow[3]));
                Assert.AreEqual(expectedRow[4]?.ToString(), actualRow[4]?.ToString());

                Assert.IsNull(expectedRow[5]);
                Assert.IsNull(actualRow[5]);

                Assert.AreEqual(expectedRow[6]?.ToString(), actualRow[6]?.ToString());

            }

        }
    }
}
