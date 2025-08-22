using MicroM.Data;

namespace MicroM.Database;

/// <summary>
/// Utilities for SQL Server database management tasks.
/// </summary>
public static class DatabaseManagement
{
    /// <summary>
    /// Determines whether the current connection has administrative rights.
    /// </summary>
    /// <param name="dbc">Database client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the user is a sysadmin; otherwise, <c>false</c>.</returns>
    public async static Task<bool> LoggedInUserHasAdminRights(IEntityClient dbc, CancellationToken ct)
    {
        return await dbc.ExecuteSQLSingleColumn<int?>("select is_srvrolemember('sysadmin')", ct) == 1;
    }

    /// <summary>
    /// Checks whether a SQL login exists on the server.
    /// </summary>
    /// <param name="dbc">Database client.</param>
    /// <param name="sql_user">Login name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the login exists.</returns>
    public async static Task<bool> UserExists(IEntityClient dbc, string sql_user, CancellationToken ct)
    {
        return await dbc.ExecuteSQLSingleColumn<int?>($"select suser_id('{sql_user ?? ""}')", ct) != null;
    }

    /// <summary>
    /// Checks if a database exists on the server.
    /// </summary>
    /// <param name="dbc">Database client.</param>
    /// <param name="sql_database">Database name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the database exists.</returns>
    public async static Task<bool> DatabaseExists(IEntityClient dbc, string sql_database, CancellationToken ct)
    {
        return await dbc.ExecuteSQLSingleColumn<int?>($"select convert(int,db_id('{sql_database ?? ""}'))", ct) != null;
    }

    /// <summary>
    /// Tests if the server is reachable and accepting connections.
    /// </summary>
    /// <param name="dbc">Database client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the connection succeeds.</returns>
    public async static Task<bool> ServerIsUp(IEntityClient dbc, CancellationToken ct)
    {
        return await dbc.Connect(ct);
    }

    /// <summary>
    /// Creates a database with the specified name and collation.
    /// </summary>
    /// <param name="dbc">Database client.</param>
    /// <param name="database_name">Name of the database.</param>
    /// <param name="database_collation">Optional collation.</param>
    /// <param name="ct">Cancellation token.</param>
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

    /// <summary>
    /// Drops the specified database if it exists.
    /// </summary>
    /// <param name="dbc">Database client.</param>
    /// <param name="database_name">Database name.</param>
    /// <param name="ct">Cancellation token.</param>
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

    /// <summary>
    /// Creates a login and database user with the supplied password.
    /// </summary>
    /// <param name="dbc">Database client.</param>
    /// <param name="database_name">Database to associate.</param>
    /// <param name="login_name">Login name.</param>
    /// <param name="password">Password.</param>
    /// <param name="ct">Cancellation token.</param>
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

    /// <summary>
    /// Drops a SQL login from the server.
    /// </summary>
    /// <param name="dbc">Database client.</param>
    /// <param name="login_name">Login name.</param>
    /// <param name="ct">Cancellation token.</param>
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

    /// <summary>
    /// Determines if a table exists in the specified schema.
    /// </summary>
    /// <param name="ec">Entity client.</param>
    /// <param name="table_name">Table name.</param>
    /// <param name="schema_name">Schema name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the table exists.</returns>
    public static async Task<bool> TableExists(IEntityClient ec, string table_name, string schema_name, CancellationToken ct)
    {
        string query = $"SELECT count(*) FROM information_schema.tables WHERE table_schema = '{schema_name}' AND table_name = '{table_name}'";
        return await ec.ExecuteSQLSingleColumn<int>(query, ct) == 1;
    }

}
