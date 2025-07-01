using MicroM.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LibraryTest;

[TestClass]
public class DatabaseSchemaCustomScriptTests
{
    [TestMethod]
    public void ExtractCustomScript_ReturnsNull_OnNullOrWhitespace()
    {
        Assert.IsNull(DatabaseSchemaCustomScripts.ExtractCustomScript(null));
        Assert.IsNull(DatabaseSchemaCustomScripts.ExtractCustomScript(""));
        Assert.IsNull(DatabaseSchemaCustomScripts.ExtractCustomScript("   "));
    }

    [TestMethod]
    public void ExtractCustomScript_ParsesCreateProc()
    {
        string sql = "CREATE PROC [dbo].[TestProc1] AS SELECT 1";
        var result = DatabaseSchemaCustomScripts.ExtractCustomScript(sql);

        Assert.IsNotNull(result);
        Assert.AreEqual("TestProc1", result.ProcName);
        Assert.AreEqual(SQLScriptType.Procedure, result.ProcType);
        Assert.AreEqual(sql, result.SQLText);
    }

    [TestMethod]
    public void ExtractCustomScript_ParsesCreateProcedureWithSchema()
    {
        string sql = "CREATE PROCEDURE dbo.[TestProc2] AS SELECT 1";
        var result = DatabaseSchemaCustomScripts.ExtractCustomScript(sql);

        Assert.IsNotNull(result);
        Assert.AreEqual("TestProc2", result.ProcName);
        Assert.AreEqual(SQLScriptType.Procedure, result.ProcType);
    }

    [TestMethod]
    public void ExtractCustomScript_ParsesAlterProc()
    {
        string sql = "ALTER PROC [TestProc3] AS SELECT 1";
        var result = DatabaseSchemaCustomScripts.ExtractCustomScript(sql);

        Assert.IsNotNull(result);
        Assert.AreEqual("TestProc3", result.ProcName);
        Assert.AreEqual(SQLScriptType.Procedure, result.ProcType);
    }

    [TestMethod]
    public void ExtractCustomScript_ParsesCreateOrAlterProc()
    {
        string sql = "CREATE OR ALTER PROC [dbo].[TestProc4] AS SELECT 1";
        var result = DatabaseSchemaCustomScripts.ExtractCustomScript(sql);

        Assert.IsNotNull(result);
        Assert.AreEqual("TestProc4", result.ProcName);
        Assert.AreEqual(SQLScriptType.Procedure, result.ProcType);
    }

    [TestMethod]
    public void ExtractCustomScript_ParsesCreateFunction()
    {
        string sql = "CREATE FUNCTION [dbo].[TestFunc1] () RETURNS INT AS BEGIN RETURN 1 END";
        var result = DatabaseSchemaCustomScripts.ExtractCustomScript(sql);

        Assert.IsNotNull(result);
        Assert.AreEqual("TestFunc1", result.ProcName);
        Assert.AreEqual(SQLScriptType.Function, result.ProcType);
    }

    [TestMethod]
    public void ExtractCustomScript_ParsesCreateOrAlterFunction()
    {
        string sql = "CREATE OR ALTER FUNCTION [dbo].[TestFunc1] () RETURNS INT AS BEGIN RETURN 1 END";
        var result = DatabaseSchemaCustomScripts.ExtractCustomScript(sql);

        Assert.IsNotNull(result);
        Assert.AreEqual("TestFunc1", result.ProcName);
        Assert.AreEqual(SQLScriptType.Function, result.ProcType);
    }

    [TestMethod]
    public void ExtractCustomScript_RemovesBlockAndLineComments()
    {
        string sql = @"
            /* Block comment */
            -- CREATE PROC ShouldNotBeFound
            CREATE PROC [TestProc5] AS SELECT 1 -- Inline comment
            ";
        var result = DatabaseSchemaCustomScripts.ExtractCustomScript(sql);

        Assert.IsNotNull(result);
        Assert.AreEqual("TestProc5", result.ProcName);
        Assert.AreEqual(SQLScriptType.Procedure, result.ProcType);
        Assert.IsFalse(result.ProcName.Contains("ShouldNotBeFound"));
    }

    [TestMethod]
    public void ExtractCustomScript_ReturnsUnknown_OnNoProc()
    {
        string sql = "SELECT * FROM SomeTable";
        var result = DatabaseSchemaCustomScripts.ExtractCustomScript(sql);

        Assert.IsNotNull(result);
        Assert.IsNull(result.ProcName);
        Assert.AreEqual(SQLScriptType.Unknown, result.ProcType);
    }

    [TestMethod]
    public void ExtractCustomScript_ParsesProcWithMnemonic()
    {
        string sql = "CREATE PROC [mneo_TestProc6] AS SELECT 1";
        var result = DatabaseSchemaCustomScripts.ExtractCustomScript(sql);

        Assert.IsNotNull(result);
        Assert.AreEqual("mneo_TestProc6", result.ProcName);
        Assert.AreEqual("mneo", result.mneo);
    }

    [TestMethod]
    public void ExtractCustomScript_Type()
    {
        string sql = "if type_id(N'EmailTagsTableType') is null\r\nbegin\r\n\r\nCREATE TYPE EmailTagsTableType \r\n   AS TABLE\r\n      ( tag VARCHAR(255)\r\n      , value varchar(max))\r\n\r\nend";
        var result = DatabaseSchemaCustomScripts.ExtractCustomScript(sql);

        Assert.IsNotNull(result);
        Assert.AreEqual("EmailTagsTableType", result.ProcName);
        Assert.AreEqual(SQLScriptType.Type, result.ProcType);
    }

    [TestMethod]
    public void ClassifyCustomSQLScript_MultipleScriptsSeparatedByGo_ReturnsAll()
    {
        string sql = @"
                CREATE PROC [dbo].[TestProc1] AS SELECT 1
                GO
                CREATE FUNCTION [dbo].[TestFunc1]() RETURNS INT AS BEGIN RETURN 1 END
                   GO  
                CREATE TYPE TestType AS TABLE (id INT)
                GO";
        var results = DatabaseSchemaCustomScripts.ClassifyCustomSQLScript(sql).ToList();

        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("TestProc1", results[0].ProcName);
        Assert.AreEqual(SQLScriptType.Procedure, results[0].ProcType);
        Assert.AreEqual("TestFunc1", results[1].ProcName);
        Assert.AreEqual(SQLScriptType.Function, results[1].ProcType);
        Assert.AreEqual("TestType", results[2].ProcName);
        Assert.AreEqual(SQLScriptType.Type, results[2].ProcType);
    }
}