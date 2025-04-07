using MicroM.Data;
using System.Threading;
using System.Threading.Tasks;
using static LibraryTest.A_DatabaseClientTests;

namespace LibraryTest
{
    public class EntityTestsUtil
    {
        public async Task CreateTestDBAsync()
        {
            using var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);
            await client.ExecuteSQLNonQuery($"create database {DatabaseConfiguration.TestDatabase}", cts.Token);
            await client.ExecuteSQLNonQuery($"alter database {DatabaseConfiguration.TestDatabase} set recovery simple", cts.Token);
            await client.Disconnect();

        }

        public async Task DeleteTestDBAsync()
        {
            using var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);
            await client.ExecuteSQL($"if DB_ID('{DatabaseConfiguration.TestDatabase}') is not null alter database {DatabaseConfiguration.TestDatabase} set single_user with rollback immediate", cts.Token);
            await client.ExecuteSQL($"if DB_ID('{DatabaseConfiguration.TestDatabase}') is not null drop database {DatabaseConfiguration.TestDatabase}", cts.Token);

            await client.ExecuteSQL($"if DB_ID('{DatabaseConfiguration.ConfigurationDatabase}') is not null alter database {DatabaseConfiguration.ConfigurationDatabase} set single_user with rollback immediate", cts.Token);
            await client.ExecuteSQL($"if DB_ID('{DatabaseConfiguration.ConfigurationDatabase}') is not null drop database {DatabaseConfiguration.ConfigurationDatabase}", cts.Token);

            await client.Disconnect();

        }

    }
}
