using MicroM.Configuration;
using MicroM.Configuration.CategoriesDefinitions;
using MicroM.Data;
using MicroM.DataDictionary.Entities;
using MicroM.DataDictionary.StatusDefinitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static LibraryTest.A_DatabaseClientTests;

namespace LibraryTest;

public static class FileStoreTests
{
    public static async Task FileStore_And_FileStoreContent_RoundTrip_3MbXlsx()
    {
        AppDBSchemaConfiguration schema_config = new("microm", "dbo");

        using var client = new DatabaseClient(
            DatabaseConfiguration.Server,
            DatabaseConfiguration.TestDatabase,
            DatabaseConfiguration.user,
            DatabaseConfiguration.password);

        var cts = new CancellationTokenSource();
        await client.Connect(cts.Token);

        var filePath = Path.Combine(AppContext.BaseDirectory, "TestData", "1mb.xlsx");
        Assert.IsTrue(File.Exists(filePath), filePath);

        byte[] expected = await File.ReadAllBytesAsync(filePath, cts.Token);

        var filesStoreProcess = new FileStoreProcess(client, schema_name: schema_config.DDSchema);
        await filesStoreProcess.InsertData(cts.Token);


        // Insert metadata with FileStore
        var fileStore = new FileStore(client, schema_name: schema_config.DDSchema);
        fileStore.Def.c_fileprocess_id.Value = filesStoreProcess.Def.c_fileprocess_id.Value; // proc turns empty into NULL
        fileStore.Def.vc_filename.Value = "1mb.xlsx";
        fileStore.Def.vc_filefolder.Value = "TEST01";
        fileStore.Def.vc_fileguid.Value = Guid.NewGuid().ToString("N");
        fileStore.Def.bi_filesize.Value = expected.LongLength;
        fileStore.Def.c_fileuploadstatus_id.Value = nameof(FileUpload.Uploaded);
        fileStore.Def.c_filestoragetype_id.Value = nameof(FileStorageTypes.SQLFileStorage);
        await fileStore.InsertData(cts.Token);

        await FileStore.UpdateStatus(client, schema_config.DDSchema, fileStore.Def.c_file_id.Value, nameof(FileUpload.Uploaded), cts.Token);

        await client.Disconnect();
        await client.Connect(cts.Token);

        // Insert the file bytes with FileStoreContent
        var content = new FileStoreContent(client, schema_name: schema_config.DDSchema);
        content.Def.c_file_id.Value = fileStore.Def.c_file_id.Value;
        await using (var fs = File.OpenRead(filePath))
        {
            content.Def.vb_file_content.Value = fs;
            await content.ExecuteProcNonQuery(content.Def.fsc_InsertFileContent, cts.Token);
        }

        await client.Disconnect();
        await client.Connect(cts.Token);

        // Read back with fsc_GetFileContent
        var read = new FileStoreContent(client, schema_name: schema_config.DDSchema);
        read.Def.c_file_id.Value = fileStore.Def.c_file_id.Value;

        var stream = await read.ExecuteProcSingleColumn<Stream>(
            read.Def.fsc_GetFileContent,
            cts.Token);

        using var memoryStream = new MemoryStream();

        await stream!.CopyToAsync(memoryStream, cts.Token);

        byte[] actual = memoryStream.ToArray();

        Assert.IsNotNull(actual);
        CollectionAssert.AreEqual(expected, actual);
    }
}

