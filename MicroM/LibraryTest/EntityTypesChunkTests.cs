using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MicroM.Core;
using MicroM.Configuration;
using MicroM.Data;
using MicroM.Database;
using MicroM.DataDictionary.Entities;
using MicroM.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LibraryTest;

[TestClass]
public class EntityTypesChunkTests
{
    [TestMethod]
    public async Task Lookup_IsEmbedded_AndUsesConfiguredSchema()
    {
        var scripts = await typeof(MicromEntitiesTypes).Assembly.GetAllClassifiedCustomSQLScripts(CancellationToken.None, "custom");
        var script = scripts.Values.Single(s => s.ProcName == "mty_GetEntitiesTypes");
        StringAssert.Contains(script.SQLText, "[custom].mty_GetEntitiesTypes");
        StringAssert.Contains(script.SQLText, "select  vc_entity_name, vc_entity_type");
        StringAssert.Contains(script.SQLText, "[custom].microm_entities_types");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ExecutionGrants_IncludeLookup_ForSystemAndConfigurationUsers(bool systemGrant)
    {
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        if (systemGrant)
            await DataDictionarySchema.GrantPermissionsToSystemProcs(client.Object, "app_sql_user", new AppDBSchemaConfiguration("custom", "app"), CancellationToken.None);
        else
            await DatabaseSchemaPermissions.GrantExecutionToAllProcs(client.Object,
                DataDictionarySchema.GetDataDictionaryEntitiesInstances(client.Object, "custom"), "config_sql_user", CancellationToken.None);

        string user = systemGrant ? "app_sql_user" : "config_sql_user";
        client.Verify(c => c.ExecuteSQLNonQuery(It.Is<string>(sql => sql.Contains("grant exec on [custom].mty_GetEntitiesTypes to [" + user + "]")), It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.ExecuteSQLNonQuery(It.Is<string>(sql => sql.Contains("grant exec on [custom].mme_InitializeMenu to [" + user + "]")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task LookupFailure_Propagates_WithoutDeletingOrInserting()
    {
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        client.Setup(c => c.ExecuteSP("[custom].mty_GetEntitiesTypes", It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Lookup failed"));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => MicromEntitiesTypes.FillEntitiesTypes(client.Object, "custom",
            new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>(), CancellationToken.None));
        client.Verify(c => c.ExecuteSP<DBStatus>(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<AutoMapperMode>(), It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<IEntityClient.MapResult<DBStatus>>()), Times.Never);
        client.Verify(c => c.Disconnect(), Times.Once);
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(201, false)]
    [DataRow(401, true)]
    public async Task FillEntitiesTypes_UsesJsonChunks_AndNeverRollsBack(int count, bool failSecondChunk)
    {
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        client.SetupGet(c => c.WebUser).Returns("audit-user");
        var rows = new DataResult(2);
        if (count == 0) rows.records.Add(["Old", "Old.Type"]);
        client.Setup(c => c.ExecuteSP("[custom].mty_GetEntitiesTypes", It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<CancellationToken>()))
            .Callback((string proc, IEnumerable<ColumnBase> parms, CancellationToken ct) => Assert.HasCount(0, parms.ToArray()))
            .ReturnsAsync(new List<DataResult> { rows });
        var calls = new List<string>();
        var insertedNames = new List<string>();
        var sizes = new List<int>();
        client.Setup(c => c.ExecuteSP<DBStatus>(It.IsAny<string>(), It.IsAny<CancellationToken>(),
                It.IsAny<AutoMapperMode>(), It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<IEntityClient.MapResult<DBStatus>>()))
            .Returns((string proc, CancellationToken ct, AutoMapperMode mode, IEnumerable<ColumnBase> parms, IEntityClient.MapResult<DBStatus> mapper) =>
            {
                calls.Add(proc);
                if (proc.EndsWith("mty_InsertChunk", StringComparison.Ordinal))
                {
                    var parameters = parms.ToArray();
                    var json = parameters.Single(p => p.SQLParameterName == "json_entities");
                    Assert.AreEqual(SqlDbType.VarChar, json.SQLMetadata.SQLType);
                    Assert.AreEqual("audit-user", parameters.Single(p => p.SQLParameterName == "webusr").ValueObject);
                    using var document = JsonDocument.Parse((string)json.ValueObject!);
                    sizes.Add(document.RootElement.GetArrayLength());
                    if (failSecondChunk && sizes.Count == 2) throw new InvalidOperationException("Chunk failed");
                    foreach (var entry in document.RootElement.EnumerateArray())
                    {
                        var name = entry.GetProperty("vc_entity_name").GetString()!;
                        Assert.AreEqual("ChunkEntities." + name, entry.GetProperty("vc_entity_type").GetString());
                        insertedNames.Add(name);
                    }
                }
                return Task.FromResult(new List<DBStatus> { new(DBStatusCodes.OK, "OK") });
            });

        var entities = new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>();
        var module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule("entities");
        for (int i = 0; i < count; i++)
        {
            var type = module.DefineType("ChunkEntities.Entity" + i, TypeAttributes.Public, typeof(MicromEntitiesTypes)).CreateType()!;
            entities.Add(type.Name, new((EntityBase)Activator.CreateInstance(type)!));
        }
        if (failSecondChunk)
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => MicromEntitiesTypes.FillEntitiesTypes(client.Object, "custom", entities, CancellationToken.None));
        else
            await MicromEntitiesTypes.FillEntitiesTypes(client.Object, "custom", entities, CancellationToken.None);

        Assert.AreEqual("[custom].mty_deleteAll", calls[0]);
        Assert.AreEqual(1, calls.Count(c => c.EndsWith("mty_deleteAll", StringComparison.Ordinal)));
        Assert.IsTrue(calls.Skip(1).All(c => c == "[custom].mty_InsertChunk"));
        CollectionAssert.AreEqual(failSecondChunk ? new[] { 200, 200 } : count == 0 ? Array.Empty<int>() : new[] { 200, 1 }, sizes);
        Assert.AreEqual(failSecondChunk ? 200 : count, insertedNames.Count);
        client.Verify(c => c.BeginTransaction(It.IsAny<CancellationToken>()), Times.Never);
        client.Verify(c => c.CommitTransaction(It.IsAny<CancellationToken>()), Times.Never);
        client.Verify(c => c.RollbackTransaction(It.IsAny<CancellationToken>()), Times.Never);
        client.Verify(c => c.ExecuteSP("[custom].mty_GetEntitiesTypes", It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.ExecuteSQL(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        client.Verify(c => c.Disconnect(), Times.Once);
    }

    [TestMethod]
    public async Task InsertChunk_IsEmbedded_AndUsesConfiguredSchema()
    {
        var scripts = await typeof(MicromEntitiesTypes).Assembly.GetAllClassifiedCustomSQLScripts(CancellationToken.None, "custom");
        var script = scripts.Values.Single(s => s.ProcName == "mty_InsertChunk");
        StringAssert.Contains(script.SQLText, "[custom].mty_InsertChunk");
        StringAssert.Contains(script.SQLText, "[custom].microm_entities_types");
        StringAssert.Contains(script.SQLText, "@json_entities varchar(max)");
        StringAssert.Contains(script.SQLText, "openjson(@json_entities)");
    }
}
