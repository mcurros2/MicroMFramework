using MicroM.Database;
using MicroM.DataDictionary.Entities;
using MicroM.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryTest;

[TestClass]
public class EmbeddedResourcesExtensionsTests
{
    private const string AppSchema = "app_schema";
    private const string TransformMarker = "-- custom SQL transform applied";

    [TestMethod]
    public async Task GetAllCustomProcs_AppliesTransformAfterSchemaReplacement()
    {
        var entity = new MicromUsers(AppSchema);
        bool receivedSchemaReplacement = false;

        var scripts = await entity.GetAllCustomProcs("usr", CancellationToken.None, sql =>
        {
            receivedSchemaReplacement |= sql.Contains($"[{AppSchema}].", StringComparison.Ordinal);
            return $"{TransformMarker}\n{sql}";
        });

        Assert.IsTrue(receivedSchemaReplacement);
        Assert.IsNotEmpty(scripts);
        Assert.IsTrue(scripts.All(script => script.StartsWith(TransformMarker, StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task GetAssemblyCustomProcs_NullTransformResultKeepsSchemaReplacedSQL()
    {
        var unchangedScripts = await typeof(MicromUsers).Assembly.GetAssemblyCustomProcs(
            "usr",
            null,
            CancellationToken.None,
            AppSchema);

        var scripts = await typeof(MicromUsers).Assembly.GetAssemblyCustomProcs(
            "usr",
            null,
            CancellationToken.None,
            AppSchema,
            _ => null);

        Assert.IsNotEmpty(scripts);
        CollectionAssert.AreEqual(unchangedScripts, scripts);
        Assert.IsTrue(scripts.Any(script => script.Contains($"[{AppSchema}].", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task GetAssemblyCustomProcs_PropagatesTransformException()
    {
        await Assert.ThrowsExactlyAsync<FormatException>(async () =>
            await typeof(MicromUsers).Assembly.GetAssemblyCustomProcs(
                "usr",
                null,
                CancellationToken.None,
                AppSchema,
                _ => throw new FormatException("Invalid database mapping.")));
    }

    [TestMethod]
    public async Task GetAllClassifiedCustomSQLScripts_ClassifiesTransformedSQL()
    {
        var scripts = await typeof(MicromUsers).Assembly.GetAllClassifiedCustomSQLScripts(
            CancellationToken.None,
            AppSchema,
            sql => $"{TransformMarker}\n{sql}");

        var procedure = scripts.Values.Single(script => script.ProcName == "usr_brwStandard");
        var function = scripts.Values.Single(script => script.ProcName == "usr_tfGetUserEmails");

        Assert.AreEqual(SQLScriptType.Procedure, procedure.ProcType);
        Assert.AreEqual(SQLScriptType.Function, function.ProcType);
        Assert.StartsWith(TransformMarker, procedure.SQLText);
        Assert.StartsWith(TransformMarker, function.SQLText);
        StringAssert.Contains(procedure.SQLText, $"[{AppSchema}].");
        StringAssert.Contains(function.SQLText, $"[{AppSchema}].");
    }
}
