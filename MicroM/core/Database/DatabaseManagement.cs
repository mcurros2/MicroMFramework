﻿using MicroM.Core;
using MicroM.Data;
using System.Globalization;
using System.Text;

namespace MicroM.Database
{
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

        public static string GrantExecutionToAllProcs<T>(string login_or_group_name) where T : EntityBase, new()
        {
            StringBuilder sb = new();

            T entity = new();

            sb.AppendFormat(CultureInfo.InvariantCulture, "if(select object_id('{0}_update')) is not null grant exec on [{0}_update] to [{1}]\n", entity.Def.Mneo, login_or_group_name);
            sb.AppendFormat(CultureInfo.InvariantCulture, "if(select object_id('{0}_get')) is not null grant exec on [{0}_get] to [{1}]\n", entity.Def.Mneo, login_or_group_name);
            sb.AppendFormat(CultureInfo.InvariantCulture, "if(select object_id('{0}_drop')) is not null grant exec on [{0}_drop] to [{1}]\n", entity.Def.Mneo, login_or_group_name);
            sb.AppendFormat(CultureInfo.InvariantCulture, "if(select object_id('{0}_lookup')) is not null grant exec on [{0}_lookup] to [{1}]\n", entity.Def.Mneo, login_or_group_name);

            foreach (var proc in entity.Def.Procs.Values)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "if(select object_id('{0}')) is not null grant exec on [{0}] to [{1}]\n", proc.Name, login_or_group_name);
            }

            foreach (var view in entity.Def.Views.Values)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "if(select object_id('{0}')) is not null grant exec on [{0}] to [{1}]\n", view.Proc.Name, login_or_group_name);
            }

            return sb.ToString();
        }




    }
}
