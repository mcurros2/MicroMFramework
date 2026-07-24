using MicroM.Data;

namespace MicroM.Database;

public class ExistingConstraintInfo
{
    public string ConstraintType { get; set; } = null!;
    public List<string> Columns { get; set; } = new();
}

public class ExistingIndexInfo
{
    public List<string> Columns { get; set; } = new();
    public bool IsUnique { get; set; }
}


public static class DatabaseManagement
{
    public async static Task<bool> LoggedInUserHasAdminRights(IEntityClient dbc, CancellationToken ct)
    {
        return await dbc.ExecuteSQLSingleColumn<int?>("select is_srvrolemember('sysadmin')", ct) == 1;
    }

    public async static Task<bool> UserExists(IEntityClient dbc, string sql_user, CancellationToken ct)
    {
        return await dbc.ExecuteSQLSingleColumn<int?>($"select suser_id('{sql_user ?? ""}')", ct) != null;
    }

    public async static Task<bool> DatabaseExists(IEntityClient dbc, string sql_database, CancellationToken ct)
    {
        return await dbc.ExecuteSQLSingleColumn<int?>($"select convert(int,db_id('{sql_database ?? ""}'))", ct) != null;
    }

    public async static Task<bool> ServerIsUp(IEntityClient dbc, CancellationToken ct)
    {
        return await dbc.Connect(ct);
    }

    public async static Task CreateDatabase(IEntityClient dbc, string database_name, string? database_collation, CancellationToken ct)
    {
        using IEntityClient ec = dbc.Clone();
        try
        {
            string collate = !string.IsNullOrEmpty(database_collation) ? $" COLLATE {database_collation}" : "";

            await ec.Connect(ct);
            await ec.ExecuteSQLNonQuery($"use [master]", ct);
            await ec.ExecuteSQLNonQuery($"create database [{database_name}]{collate}", ct);
            await ec.ExecuteSQLNonQuery($"alter database [{database_name}] set recovery simple", ct);
        }
        finally
        {
            await ec.Disconnect();
        }
    }

    public static async Task DropDatabase(IEntityClient dbc, string database_name, CancellationToken ct)
    {
        using IEntityClient ec = dbc.Clone();
        try
        {
            await ec.Connect(ct);
            await ec.ExecuteSQLNonQuery($"use [{ec.MasterDatabase}]", ct);
            await ec.ExecuteSQLNonQuery($"begin try\nalter database [{database_name}] set single_user with rollback immediate\nend try\nbegin catch\nend catch", ct);
            await ec.ExecuteSQLNonQuery($"drop database if exists [{database_name}]", ct);
        }
        finally
        {
            await ec.Disconnect();
        }
    }

    public static async Task CreateLoginAndDatabaseUser(IEntityClient dbc, string database_name, string login_name, string password, CancellationToken ct)
    {
        using IEntityClient ec = dbc.Clone();
        try
        {
            await ec.Connect(ct);
            await ec.ExecuteSQLNonQuery($"use [{database_name}]", ct);
            await ec.ExecuteSQLNonQuery($"create login [{login_name}] with password = '{password}', check_expiration = off, check_policy = off, default_database = [{database_name}]", ct);
            await ec.ExecuteSQLNonQuery($"if user_id('{login_name}') is not null drop user [{login_name}]", ct);
            await ec.ExecuteSQLNonQuery($"create user [{login_name}] with default_schema = [dbo]", ct);
        }
        finally
        {
            await ec.Disconnect();
        }
    }

    public static async Task DropLogin(IEntityClient dbc, string login_name, CancellationToken ct)
    {
        using IEntityClient ec = dbc.Clone();
        try
        {
            await ec.Connect(ct);
            await ec.ExecuteSQLNonQuery($"use [master]", ct);
            await ec.ExecuteSQLNonQuery($"begin try\ndrop login [{login_name}]\nend try\nbegin catch\nend catch", ct);
        }
        finally
        {
            await ec.Disconnect();
        }
    }

    public static async Task<bool> IsDBOwner(IEntityClient dbc, string database_name, string owner_name, CancellationToken ct)
    {
        using IEntityClient ec = dbc.Clone();
        try
        {
            await ec.Connect(ct);
            await ec.ExecuteSQLNonQuery($"use [{database_name}]", ct);
            return await ec.ExecuteSQLSingleColumn<int?>($"select is_member('db_owner', '{owner_name}')", ct) == 1;
        }
        catch
        {
            return false;
        }
        finally
        {
            await ec.Disconnect();
        }
    }

    public static async Task EnsureDbOwnerMembership(IEntityClient dbc, string database_name, string login_name, CancellationToken ct)
    {
        using IEntityClient ec = dbc.Clone();
        try
        {
            await ec.Connect(ct);
            await ec.ExecuteSQLNonQuery($"use [{database_name}]", ct);
            await ec.ExecuteSQLNonQuery($"if user_id('{login_name}') is null create user [{login_name}] for login [{login_name}] with default_schema = [dbo]", ct);
            await ec.ExecuteSQLNonQuery($"begin try alter role [db_owner] add member [{login_name}] end try begin catch end catch", ct);
        }
        finally
        {
            await ec.Disconnect();
        }
    }

    public static async Task<bool> TableExists(IEntityClient ec, string table_name, string schema_name, CancellationToken ct)
    {
        string query = $"SELECT count(*) FROM information_schema.tables WHERE table_schema = '{schema_name}' AND table_name = '{table_name}'";
        return await ec.ExecuteSQLSingleColumn<int>(query, ct) == 1;
    }

    public static async Task<bool> SchemaExists(IEntityClient ec, string schema_name, CancellationToken ct)
    {
        string query = $"SELECT count(*) FROM information_schema.schemata WHERE schema_name = '{schema_name}'";
        return await ec.ExecuteSQLSingleColumn<int>(query, ct) == 1;
    }

    public static async Task<List<string>?> GetExistingPrimaryKeyColumns(IEntityClient ec, string full_table_name, CancellationToken ct)
    {
        string sql = $@"
SELECT c.name
FROM sys.key_constraints kc
INNER JOIN sys.index_columns ic ON kc.parent_object_id = ic.object_id AND kc.unique_index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE kc.parent_object_id = OBJECT_ID('{full_table_name}')
  AND kc.type = 'PK'
ORDER BY ic.key_ordinal";

        var results = await ec.ExecuteSQL(sql, ct);
        if (results.Count == 0 || results[0].records.Count == 0) return null;

        return [.. results[0].records.Select(r => r[0]?.ToString() ?? "")];
    }

    public static async Task<List<ExistingConstraintInfo>> GetExistingUniqueConstraints(IEntityClient ec, string full_table_name, CancellationToken ct)
    {
        string sql = $@"
SELECT kc.name AS constraint_name, c.name AS column_name, ic.key_ordinal
FROM sys.key_constraints kc
INNER JOIN sys.index_columns ic ON kc.parent_object_id = ic.object_id AND kc.unique_index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE kc.parent_object_id = OBJECT_ID('{full_table_name}')
  AND kc.type = 'UQ'
ORDER BY kc.name, ic.key_ordinal";

        var results = await ec.ExecuteSQL(sql, ct);
        if (results.Count == 0 || results[0].records.Count == 0) return [];

        var grouped = results[0].records
            .GroupBy(r => r[0]?.ToString() ?? "")
            .Select(g => new ExistingConstraintInfo
            {
                ConstraintType = "UQ",
                Columns = [.. g.Select(r => r[1]?.ToString() ?? "")]
            })
            .ToList();

        return grouped;
    }

    public static async Task<List<ExistingConstraintInfo>> GetExistingForeignKeys(IEntityClient ec, string full_table_name, CancellationToken ct)
    {
        string sql = $@"
SELECT fk.name AS fk_name, c.name AS column_name, fkc.constraint_column_id
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
WHERE fk.parent_object_id = OBJECT_ID('{full_table_name}')
ORDER BY fk.name, fkc.constraint_column_id";

        var results = await ec.ExecuteSQL(sql, ct);
        if (results.Count == 0 || results[0].records.Count == 0) return [];

        var grouped = results[0].records
            .GroupBy(r => r[0]?.ToString() ?? "")
            .Select(g => new ExistingConstraintInfo
            {
                ConstraintType = "FK",
                Columns = [.. g.Select(r => r[1]?.ToString() ?? "")]
            })
            .ToList();

        return grouped;
    }

    public static async Task<List<ExistingIndexInfo>> GetExistingIndexes(IEntityClient ec, string full_table_name, CancellationToken ct)
    {
        string sql = $@"
SELECT i.name AS index_name, c.name AS column_name, ic.key_ordinal, i.is_unique
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('{full_table_name}')
  AND i.is_primary_key = 0
  AND i.is_unique_constraint = 0
  AND ic.is_included_column = 0
ORDER BY i.name, ic.key_ordinal";

        var results = await ec.ExecuteSQL(sql, ct);
        if (results.Count == 0 || results[0].records.Count == 0) return [];

        var grouped = results[0].records
            .GroupBy(r => r[0]?.ToString() ?? "")
            .Select(g => new ExistingIndexInfo
            {
                Columns = [.. g.Select(r => r[1]?.ToString() ?? "")],
                IsUnique = Convert.ToBoolean(g.First()[3])
            })
            .ToList();

        return grouped;
    }

    public static bool AreColumnsNamesAndOrderEqual(IEnumerable<string> columns1, IEnumerable<string> columns2)
    {
        return columns1.SequenceEqual(columns2, StringComparer.OrdinalIgnoreCase);
    }

    public static bool ConstraintExistsWithTheSameColumns(List<ExistingConstraintInfo> existingConstraints, IEnumerable<string> columns)
    {
        return existingConstraints.Any(c => AreColumnsNamesAndOrderEqual(c.Columns, columns));
    }

    public static bool IndexExistsWithTheSameColumns(List<ExistingIndexInfo> existingIndexes, IEnumerable<string> columns)
    {
        return existingIndexes.Any(idx => AreColumnsNamesAndOrderEqual(idx.Columns, columns));
    }


}
