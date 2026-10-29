using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MicroM.Configuration;
using MicroM.Configuration.Entities;
using MicroM.Data;
using MicroM.Database;
using MicroM.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LibraryTest;

[TestClass]
public class AssemblyTypesLookupTests
{
    [TestMethod]
    [DataRow("0000000001")]
    [DataRow("9999999999")]
    public async Task Lookup_PassesAssemblyIdUnchanged_AsChar20(string assemblyId)
    {
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        var rows = new List<DataResult> { new(1) };
        client.Setup(c => c.ExecuteSP("[custom].eat_GetAssemblyTypes", It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<CancellationToken>()))
            .Callback((string proc, IEnumerable<ColumnBase> parms, CancellationToken ct) =>
            {
                var parameter = parms.Single();
                Assert.AreEqual("assembly_id", parameter.SQLParameterName);
                Assert.AreEqual(SqlDbType.Char, parameter.SQLMetadata.SQLType);
                Assert.AreEqual(20, parameter.SQLMetadata.Size);
                Assert.AreEqual(assemblyId, parameter.ValueObject);
                Assert.IsFalse(parameter.SQLMetadata.Output);
            }).ReturnsAsync(rows);

        var entity = new EntitiesAssembliesTypes(client.Object, schema_name: "custom");
        entity.Def.c_assembly_id.Value = assemblyId;
        var result = await entity.ExecuteProc(entity.Def.eat_GetAssemblyTypes, CancellationToken.None);

        Assert.AreSame(rows, result);
        client.Verify(c => c.ExecuteSP("[custom].eat_GetAssemblyTypes", It.IsAny<IEnumerable<ColumnBase>>(), It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.ExecuteSQL(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ConfigurationGrants_IncludeLookup_ForConfiguredUser()
    {
        var client = new Mock<IEntityClient>();
        client.SetupGet(c => c.ConnectionState).Returns(ConnectionState.Open);
        var entities = ConfigurationDatabaseSchema.GetConfigurationEntitiesInstances(client.Object, "custom");
        await DatabaseSchemaPermissions.GrantExecutionToAllProcs(client.Object, entities, "configured_sql_user", CancellationToken.None);

        client.Verify(c => c.ExecuteSQLNonQuery(It.Is<string>(sql =>
            sql.Contains("grant exec on [custom].eat_GetAssemblyTypes to [configured_sql_user]")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Lookup_IsEmbedded_AndUsesTheConfigurationSchema()
    {
        var scripts = await typeof(EntitiesAssembliesTypes).Assembly.GetAllClassifiedCustomSQLScripts(CancellationToken.None, "custom");
        var script = scripts.Values.Single(s => s.ProcName == "eat_GetAssemblyTypes");
        StringAssert.Contains(script.SQLText, "[custom].eat_GetAssemblyTypes");
        StringAssert.Contains(script.SQLText, "@assembly_id char(20)");
        StringAssert.Contains(script.SQLText, "[custom].entities_assemblies_types");
        StringAssert.Contains(script.SQLText, "where   c_assembly_id = @assembly_id");
    }
}
