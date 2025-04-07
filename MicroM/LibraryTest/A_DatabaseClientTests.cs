using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryTest
{
    [TestClass]
    public class A_DatabaseClientTests
    {
        public class DatabaseConfiguration
        {
            public static string Server { get; private set; }
            public static string SystemDatabase { get; private set; }
            public static string TestDatabase { get; private set; }
            public static string user { get; private set; }
            public static string password { get; private set; }
            public static string ConfigurationDatabase { get; private set; }
            public static string ConfigurationUser { get; private set; }
            public static string ConfigurationPassword { get; private set; }
            public static string CertificatePassword { get; private set; }
            public static string CertificateSubjectName { get; private set; }

            static DatabaseConfiguration()
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddUserSecrets<A_DatabaseClientTests>()
                    .Build();

                Server = configuration["DatabaseConfiguration:Server"] ?? ".\\";
                SystemDatabase = configuration["DatabaseConfiguration:SystemDatabase"] ?? "master";
                TestDatabase = configuration["DatabaseConfiguration:TestDatabase"] ?? "MicroM_tests";
                user = configuration["DatabaseConfiguration:User"] ?? "sa";
                password = configuration["DatabaseConfiguration:Password"] ?? "";
                ConfigurationDatabase = configuration["DatabaseConfiguration:ConfigurationDatabase"] ?? $"{ConfigurationDefaults.SQLConfigDatabaseName}_test";
                ConfigurationUser = configuration["DatabaseConfiguration:ConfigurationUser"] ?? $"{ConfigurationDefaults.SQLConfigUser}_test";
                ConfigurationPassword = configuration["DatabaseConfiguration:ConfigurationPassword"] ?? "";

                // This is not sensible, will just create a local certificate for the test if not exists
                CertificatePassword = configuration["DatabaseConfiguration:CertificatePassword"] ?? "cert123456";

                CertificateSubjectName = configuration["DatabaseConfiguration:CertificateSubjectName"] ?? $"{ConfigurationDefaults.CertificateSubjectName}_test";
            }
        }

        [TestMethod]
        public void TestRandomPassword()
        {
            int length = 10;
            int minSymbols = 2;
            int minNumbers = 2;
            int minUppercase = 2;
            int minLowercase = 2;

            string password = CryptClass.CreateRandomPassword(length, minSymbols, minNumbers, minUppercase, minLowercase);

            Assert.AreEqual(length, password.Length);
            Assert.IsTrue(password.Count(c => char.IsSymbol(c) || char.IsPunctuation(c)) >= minSymbols);
            Assert.IsTrue(password.Count(c => char.IsNumber(c)) >= minNumbers);
            Assert.IsTrue(password.Count(c => char.IsUpper(c)) >= minUppercase);
            Assert.IsTrue(password.Count(c => char.IsLower(c)) >= minLowercase);

        }


        [TestMethod]
        public async Task ConnectAndDisconnectToDatabase_ReturnsCorrectState()
        {
            var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();
            await client.Connect(cts.Token);
            Assert.AreEqual(ConnectionState.Open, client.ConnectionState, $"Unexpected connection state after Connect. Expected: Open, Actual: {client.ConnectionState}.");
            await client.Disconnect();
            Assert.AreEqual(ConnectionState.Closed, client.ConnectionState, $"Unexpected connection state after Disconnect. Expected: Closed, Actual: {client.ConnectionState}.");

        }

        [TestMethod]
        public async Task ExecuteSQLChannel_ReturnsCorrectNumberOfResultsAndRecords()
        {
            var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            var result_set = new DataResultSetChannel();

            await client.ExecuteSQLChannel("select top 10 * from sysobjects\nselect top 10 * from sysobjects", result_set, cts.Token);

            int results_count = 0;
            int total_records = 0;
            while (await result_set.Results.Reader.WaitToReadAsync(cts.Token))
            {
                results_count++;
                var result = await result_set.Results.Reader.ReadAsync(cts.Token);
                Debug.Print($"Result # {results_count} {DateTime.Now:O}");

                await foreach (var record in result.Records.Reader.ReadAllAsync(cts.Token))
                {
                    Debug.Print(string.Join(", ", record));
                    total_records++;
                }
                Debug.Print($"Total records: {total_records}");
            }

            Assert.AreEqual<int>(2, results_count, "ExecuteSQLChannel has not returned the correct results count");
            Assert.AreEqual<int>(20, total_records, "ExecuteSQLChannel has not returned the correct records count");


        }

        [TestMethod]
        public async Task ExecuteSQL_ReturnsCorrectNumberOfResultsAndRecords()
        {
            var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            var result_set = await client.ExecuteSQL("select top 10 * from sysobjects\nselect top 10 * from sysobjects", cts.Token);

            int results_count = result_set.Count;
            int total_records = 0;

            foreach (DataResult res in result_set)
            {
                total_records += res.records.Count;

                foreach (var record in res.records)
                {
                    Debug.Print(string.Join(", ", record));
                }
            }

            Assert.AreEqual<int>(2, results_count, "ExecuteSQL has not returned the correct results count");
            Assert.AreEqual<int>(20, total_records, "ExecuteSQL has not returned the correct records count");


        }


        [TestMethod]
        public async Task ExecuteSQLChannel_SlowClientDontKeepQueryRunningOnServer()
        {
            var cts = new CancellationTokenSource();

            var result_set_channel = new DataResultSetChannel();
            string test_id = Guid.NewGuid().ToString();

            DateTime query_started = DateTime.MinValue, query_running = DateTime.MinValue, query_completed = DateTime.MinValue, results_processed = DateTime.MinValue;

            // Main query, has a small delay to ensure query is running
            var query_task = Task.Run(async () =>
            {

                query_started = DateTime.Now;
                await DatabaseClientTestsUtil.ExecuteTestQueryChannel(test_id, "waitfor delay '00:00:02'\nselect top 10 * from sysobjects", result_set_channel, cts.Token);

            }).ContinueWith(async (t) =>
            {
                // check after query completed that is not still running on server

                query_completed = DateTime.Now;

                bool has_completed = await DatabaseClientTestsUtil.TestQueryCompleted(test_id, cts.Token);

                Debug.Print($"Test #{test_id}# finished {DateTime.Now:O}");

                Assert.IsTrue(has_completed, "The query is still runing, it should have finished first.");



            });

            // ensure query is running
            _ = Task.Delay(1000).ContinueWith(async (t) =>
            {

                bool has_completed = await DatabaseClientTestsUtil.TestQueryCompleted(test_id, cts.Token);

                Debug.Print($"Check test is running #{test_id}# finshed {DateTime.Now:O}");

                Assert.IsFalse(has_completed, $"The query #{test_id}# failed to run.");

                query_running = DateTime.Now;

            });


            // slow client
            await Task.Run(async () =>
            {

                int results_count = 0;
                int total_records = 0;
                while (await result_set_channel.Results.Reader.WaitToReadAsync(cts.Token))
                {
                    results_count++;
                    var result = await result_set_channel.Results.Reader.ReadAsync(cts.Token);
                    Debug.Print($"Result # {results_count} {DateTime.Now:O}");

                    await foreach (var record in result.Records.Reader.ReadAllAsync(cts.Token))
                    {
                        Debug.Print(string.Join(", ", record));
                        total_records++;
                        await Task.Delay(500);
                    }
                    Debug.Print($"Total records: {total_records} {DateTime.Now:O}");
                }

                results_processed = DateTime.Now;
            }).ContinueWith((t) =>
            {
                Assert.IsTrue(query_completed < results_processed, "Results have been processed before the query completed, the client should have finished after the query.");

                Debug.Print($"Query started {query_started:HH:mm:ss:ffff}, Query running check {query_running:HH:mm:ss:ffff}, Query completed {query_completed:HH:mm:ss:ffff}, Results processed {results_processed:HH:mm:ss:ffff}");

            });


        }

        [TestMethod]
        public async Task ExecuteSQLChannel_SlowQueryFastClient()
        {
            var cts = new CancellationTokenSource();

            var result_set_1 = new DataResultSetChannel();
            var result_set_channel = new DataResultSetChannel();
            string test_id = Guid.NewGuid().ToString();

            DateTime query_started = DateTime.MinValue, results_processed = DateTime.MinValue;
            int seconds_delay = 2;

            // Main query
            _ = Task.Run(async () =>
            {

                query_started = DateTime.Now;

                // this will trick sql server to send the first result before executing the next statement
                // so we can simulate processing the first result before the query finish.
                // this would happen normally with a large query with multiple results
                string sql = "select top 10 * from sysobjects; RAISERROR('', 0, 1) WITH NOWAIT; ";
                sql += $"waitfor delay '00:00:{seconds_delay:D2}'; select top 10 * from sys.all_columns";

                await DatabaseClientTestsUtil.ExecuteTestQueryChannel(test_id, sql, result_set_channel, cts.Token);

            });


            // fast client
            await Task.Run(async () =>
            {

                int results_count = 0;
                int total_records = 0;
                bool first_result_processed = false;
                while (await result_set_channel.Results.Reader.WaitToReadAsync(cts.Token))
                {
                    results_count++;
                    var result = await result_set_channel.Results.Reader.ReadAsync(cts.Token);
                    Debug.Print($"Result # {results_count} {DateTime.Now:O}");

                    await foreach (var record in result.Records.Reader.ReadAllAsync(cts.Token))
                    {
                        Debug.Print(string.Join(", ", record));
                        total_records++;
                    }
                    Debug.Print($"Total records: {total_records} {DateTime.Now:O}");
                    if (!first_result_processed)
                    {
                        results_processed = DateTime.Now;
                        first_result_processed = true;
                    }
                }

            }).ContinueWith((t) =>
            {

                Debug.Print($"Query started {query_started:HH:mm:ss:ffff}, First result processed {results_processed:HH:mm:ss:ffff}, Query completed {DateTime.Now:HH:mm:ss:ffff}");
                Assert.IsTrue((results_processed - query_started).TotalSeconds < seconds_delay, "Results where not received before query completed.");

            });

        }

        [TestMethod]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task ExecuteSQLChannel_CancelQueryWhenReturningResultsShouldNotKeepQueryRunningOnServer()
        {
            var cts = new CancellationTokenSource();

            var result_set_channel = new DataResultSetChannel();
            string test_id = Guid.NewGuid().ToString();
            int seconds_delay = 5;
            int cancel_after_millisecs = 3000;
            int check_delay_millisecs = 3500;

            DateTime query_started = DateTime.MinValue, query_running = DateTime.MinValue, query_completed = DateTime.MinValue, results_processed = DateTime.MinValue;

            // Main query, has a small delay to ensure query is running
            _ = Task.Run(async () =>
            {

                query_started = DateTime.Now;
                // this will trick sql server to send the first result before executing the next statement
                // so we can simulate processing the first result before the query finish.
                // this would happen normally with a large query with multiple results
                string sql = "select top 10 * from sysobjects; RAISERROR('', 0, 1) WITH NOWAIT; ";
                sql += $"waitfor delay '00:00:{seconds_delay:D2}'; select top 10 * from sys.all_columns; waitfor delay '00:00:{seconds_delay:D2}'; select top 10 * from sys.all_columns";
                await DatabaseClientTestsUtil.ExecuteTestQueryChannel(test_id, sql, result_set_channel, cts.Token);

            });

            cts.CancelAfter(cancel_after_millisecs);

            // ensure query has been cancelled and not running
            await Task.Delay(check_delay_millisecs).ContinueWith(async (t) =>
            {
                var cts = new CancellationTokenSource();
                bool has_completed = await DatabaseClientTestsUtil.TestQueryCompleted(test_id, cts.Token);

                Debug.Print($"Query started: {query_started:O}, cancelled: {DateTime.Now:O}, is_running: {has_completed}");

                Assert.IsTrue(has_completed, $"The query #{test_id}# is still runing on server.");

                Debug.Print($"Query cancelled #{test_id}# is not running on server {DateTime.Now:O}");

            });


            // slow client
            await Task.Run(async () =>
            {

                int results_count = 0;
                int total_records = 0;
                while (await result_set_channel.Results.Reader.WaitToReadAsync(cts.Token))
                {
                    results_count++;
                    var result = await result_set_channel.Results.Reader.ReadAsync(cts.Token);
                    Debug.Print($"Result # {results_count} {DateTime.Now:O}");

                    await foreach (var record in result.Records.Reader.ReadAllAsync(cts.Token))
                    {
                        Debug.Print(string.Join(", ", record));
                        total_records++;
                        await Task.Delay(500);
                    }
                    Debug.Print($"Total records: {total_records} {DateTime.Now:O}");
                }

                results_processed = DateTime.Now;
            });


        }

        [TestMethod]
        public async Task ExecuteSQLChannel_CancelQueryBeforeResultsShouldNotKeepQueryRunningOnServer()
        {
            var cts = new CancellationTokenSource();

            var result_set_channel = new DataResultSetChannel();
            string test_id = Guid.NewGuid().ToString();
            int seconds_delay = 10;
            int cancel_after_millisecs = 3000;
            int check_delay_millisecs = 3500;

            DateTime query_started = DateTime.MinValue, query_running = DateTime.MinValue, query_completed = DateTime.MinValue, results_processed = DateTime.MinValue;

            // Main query
            _ = Task.Run(async () =>
            {
                try
                {
                    query_started = DateTime.Now;
                    string sql = $"waitfor delay '00:00:{seconds_delay:D2}'";
                    await DatabaseClientTestsUtil.ExecuteTestQueryChannel(test_id, sql, result_set_channel, cts.Token);

                }
                catch (TaskCanceledException) { }
            });

            cts.CancelAfter(cancel_after_millisecs);

            // ensure query has been cancelled and not running
            await Task.Delay(check_delay_millisecs).ContinueWith(async (t) =>
            {
                var cts = new CancellationTokenSource();
                bool has_completed = await DatabaseClientTestsUtil.TestQueryCompleted(test_id, cts.Token);

                Debug.Print($"Query started: {query_started:O}, cancelled: {DateTime.Now:O}, is_running: {has_completed}");

                Assert.IsTrue(has_completed, $"The query #{test_id}# is still runing on server.");

                Debug.Print($"Query cancelled #{test_id}# is not running on server {DateTime.Now:O}");

            });


        }

        [TestMethod]
        public async Task ExecuteSPChannel_ReturnsCorrectNumberOfResultsAndRecords()
        {
            var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            var result_set = new DataResultSetChannel();

            var parms = new DatabaseClientTestsUtil.SPHELPParameters();
            parms.objname.Value = "sp_who";

            await client.ExecuteSPChannel("sp_help", parms.Columns, result_set, cts.Token);

            int results_count = 0;
            int total_records = 0;
            while (await result_set.Results.Reader.WaitToReadAsync(cts.Token))
            {
                results_count++;
                var result = await result_set.Results.Reader.ReadAsync(cts.Token);
                Debug.Print($"Result # {results_count} {DateTime.Now:O}");

                await foreach (var record in result.Records.Reader.ReadAllAsync(cts.Token))
                {
                    Debug.Print(string.Join(", ", record));
                    total_records++;
                }
                Debug.Print($"Total records: {total_records}");
            }

            Assert.AreEqual<int>(2, results_count, "ExecuteSPChannel has not returned the correct results count");
            Assert.AreEqual<int>(2, total_records, "ExecuteSPChannel has not returned the correct records count");


        }

        [TestMethod]
        public async Task ExecuteSP_CheckExecutionWithoutChannel()
        {
            var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);

            var parms = new DatabaseClientTestsUtil.SPHELPParameters();
            parms.objname.Value = "sp_who";

            var result_set = await client.ExecuteSP("sp_help", parms.Columns, cts.Token);

            await client.Disconnect();

            foreach (var res in result_set)
            {
                foreach (var record in res.records) Debug.Print(string.Join(", ", record));
            }

            Assert.IsTrue(result_set?[0].records.Count == 1, "ExecuteSP: The query has not returned results.");

            Assert.AreEqual<int>(2, result_set.Count, "ExecuteSP has not returned the correct results count");
            Assert.AreEqual<int>(2, result_set[0].records.Count + result_set[1].records.Count, "ExecuteSP has not returned the correct records count");

        }


        [TestMethod]
        public async Task ExecuteSPNonQuery_CheckExecutionWithoutReturningResults()
        {
            var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);

            var parms = new DatabaseClientTestsUtil.SPHELPParameters();
            parms.objname.Value = "sp_who";

            await client.ExecuteSPNonQuery("sp_help", parms.Columns, cts.Token);

            await client.Disconnect();

        }

        [TestMethod]
        public async Task ExecuteSQLNonQuery_CheckConnectionRemainsOpen()
        {
            var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);

            await client.ExecuteSQLNonQuery("sp_help", cts.Token);

            Assert.IsTrue(client.ConnectionState == ConnectionState.Open, $"ExecuteSQLNonQuery: The connection has not remained open. Actual state: {client.ConnectionState}");

            await client.Disconnect();

            await client.ExecuteSQLNonQuery("sp_help", cts.Token);

            Assert.IsTrue(client.ConnectionState == ConnectionState.Closed, $"ExecuteSQLNonQuery: The connection has not been closed. Actual state: {client.ConnectionState}");

        }

        [TestMethod]
        public async Task DateTime_CheckFormatSerializationDeserialization()
        {
            var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);

            await client.ExecuteSQLNonQuery("create or alter proc dbo.mcsi_test_dates @date datetime2\r\nas\r\nselect\t@date\r\n", cts.Token);

            await client.ExecuteSQLNonQuery("set language spanish", cts.Token);

            CustomOrderedDictionary<ColumnBase> cols = new();
            Column<DateTime> date_col = cols.AddCol<DateTime>(name: "date", sql_type: SqlDbType.DateTime2, value: DateTime.Now);

            var result_set = await client.ExecuteSP("dbo.mcsi_test_dates", cols.Values, cts.Token);

            await client.ExecuteSQLNonQuery("drop proc dbo.mcsi_test_dates", cts.Token);

            await client.Disconnect();

            foreach (var res in result_set)
            {
                foreach (var record in res.records) Debug.Print(string.Join(", ", record));
            }


            Assert.IsTrue(result_set?[0].records.Count == 1, "ExecuteSP: The query has not returned results.");

            Assert.AreEqual<DateTime>(date_col.Value, (DateTime)result_set[0].records[0][0], $"ExecuteSP the dates are different: expected {date_col.Value:O}, received {(DateTime)result_set[0].records[0][0]:O}");

        }

        public record GetColumnsResult(string name, string object_name, byte system_type_id) { public GetColumnsResult() : this(default, default, default) { } }

        [TestMethod]
        public async Task ExecuteSingelQuery_TestCustomMapper()
        {
            GetColumnsResult rec = new();
            var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);

            string query = "select\ta.name,\r\n\t\t[object_name]=object_name(a.object_id),\r\n\t\ta.system_type_id\r\nfrom\t\tsys.columns a";
            //delegate Task<T> MapResult<T>(IGetFieldValue record) = 

            List<(int status, GetColumnsResult)> test = new();

            var result = await client.ExecuteSQL<GetColumnsResult>(query, cts.Token, mapper: async (IGetFieldValue fv, string[] headers, CancellationToken ct) =>
            {
                GetColumnsResult result = new(
                    name: await fv.GetFieldValueAsync<string>(nameof(GetColumnsResult.name), ct),
                    object_name: await fv.GetFieldValueAsync<string>(nameof(GetColumnsResult.object_name), ct),
                    system_type_id: await fv.GetFieldValueAsync<byte>(nameof(GetColumnsResult.system_type_id), ct)
                    );
                test.Add((0, result));
                return result;
            });

            Console.WriteLine($"Rows: {result.Count}");
            foreach (var item in result)
            {
                Console.WriteLine(item);
            }

            await client.Disconnect();

        }

        [TestMethod]
        public async Task ExecuteSingelQuery_TestAutoMapper()
        {
            GetColumnsResult rec = new();
            var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);

            string query = "select\ta.name,\r\n\t\t[object_name]=object_name(a.object_id),\r\n\t\ta.system_type_id\r\nfrom\t\tsys.columns a";
            //delegate Task<T> MapResult<T>(IGetFieldValue record) = 

            var result = await client.ExecuteSQL<GetColumnsResult>(query, cts.Token);

            Console.WriteLine($"Rows: {result.Count}");
            foreach (var item in result)
            {
                Console.WriteLine(item);
            }

            await client.Disconnect();

        }

        [TestMethod]
        public async Task ExecuteSingelQuery_TestAutoMapper_byPosition()
        {
            GetColumnsResult rec = new();
            var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);

            string query = "select\ta.name,\r\n\t\t[object_name]=object_name(a.object_id),\r\n\t\ta.system_type_id\r\nfrom\t\tsys.columns a";

            var result = await client.ExecuteSQL<GetColumnsResult>(query, cts.Token, IEntityClient.AutoMapperMode.ByPosition);

            Console.WriteLine($"Rows: {result.Count}");
            foreach (var item in result)
            {
                Console.WriteLine(item);
            }

            await client.Disconnect();

        }

        [TestMethod]
        public async Task ExecuteSingelQuery_TestAutoMapper_ExtraColumns()
        {
            GetColumnsResult rec = new();
            var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);

            string query = "select\ta.name,\r\n\t\t[object_name]=object_name(a.object_id),\r\n\t\ta.system_type_id, *\r\nfrom\t\tsys.columns a";

            var result = await client.ExecuteSQL<GetColumnsResult>(query, cts.Token, IEntityClient.AutoMapperMode.ByPosition);

            Console.WriteLine($"Rows: {result.Count}");
            foreach (var item in result)
            {
                Console.WriteLine(item);
            }

            await client.Disconnect();

        }

        private DatabaseClient CreatePooledDatabaseClient(bool pooling, int minPoolSize, int maxPoolSize)
        {
            var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);
            client.Pooling = pooling;
            client.MinPoolSize = minPoolSize;
            client.MaxPoolSize = maxPoolSize;
            return client;
        }

        [TestMethod]
        public async Task ConnectionPooling_OpenCloseReopenConnection()
        {
            var client = CreatePooledDatabaseClient(pooling: true, minPoolSize: 1, maxPoolSize: 5);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);
            Assert.AreEqual(ConnectionState.Open, client.ConnectionState, "Connection should be open.");

            var initialSpid = await client.ExecuteSQLSingleColumn<short>("SELECT @@SPID", cts.Token);

            await client.Disconnect();
            Assert.AreEqual(ConnectionState.Closed, client.ConnectionState, "Connection should be closed.");

            await client.Connect(cts.Token);
            Assert.AreEqual(ConnectionState.Open, client.ConnectionState, "Connection should be reopened from the pool.");

            var reopenedSpid = await client.ExecuteSQLSingleColumn<short>("SELECT @@SPID", cts.Token);
            Assert.AreEqual(initialSpid, reopenedSpid, "SPID should be the same after reopening the connection from the pool.");

            await client.Disconnect();
        }

        [TestMethod]
        public async Task ConnectionPooling_NoOpenTransactionsAfterReopen()
        {
            var client = CreatePooledDatabaseClient(pooling: true, minPoolSize: 1, maxPoolSize: 5);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);

            var initialSpid = await client.ExecuteSQLSingleColumn<short>("SELECT @@SPID", cts.Token);

            await client.BeginTransaction(cts.Token);
            await client.Disconnect();

            await client.Connect(cts.Token);
            var reopenedSpid = await client.ExecuteSQLSingleColumn<short>("SELECT @@SPID", cts.Token);
            Assert.AreEqual(initialSpid, reopenedSpid, "SPID should be the same after reopening the connection from the pool.");

            var transactions = await client.ExecuteSQLSingleColumn<int>("select @@trancount", cts.Token);
            Assert.IsTrue(transactions == 0, $"There should be no open transactions after reopening the connection from the pool. Actrual trancount {transactions}");

            await client.Disconnect();
        }


        [TestMethod]
        public async Task ConnectionPooling_DefaultIsolationLevelAfterReopen()
        {
            var client = CreatePooledDatabaseClient(pooling: true, minPoolSize: 1, maxPoolSize: 5);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);
            var initialSpid = await client.ExecuteSQLSingleColumn<short>("SELECT @@SPID", cts.Token);

            await client.BeginTransaction(cts.Token);
            await client.ExecuteSQLNonQuery("SET TRANSACTION ISOLATION LEVEL SERIALIZABLE", cts.Token);
            await client.CommitTransaction(cts.Token);
            await client.Disconnect();

            await client.Connect(cts.Token);

            var reopenedSpid = await client.ExecuteSQLSingleColumn<short>("SELECT @@SPID", cts.Token);
            Assert.AreEqual(initialSpid, reopenedSpid, "SPID should be the same after reopening the connection from the pool.");

            var result = await client.ExecuteSQLSingleColumn<string>("SELECT CASE transaction_isolation_level \r\n    WHEN 0 THEN 'Unspecified' \r\n    WHEN 1 THEN 'ReadUncommitted' \r\n    WHEN 2 THEN 'ReadCommitted' \r\n    WHEN 3 THEN 'Repeatable' \r\n    WHEN 4 THEN 'Serializable' \r\n    WHEN 5 THEN 'Snapshot' END AS TRANSACTION_ISOLATION_LEVEL \r\nFROM sys.dm_exec_sessions \r\nwhere session_id = @@SPID", cts.Token);
            Assert.AreEqual("ReadCommitted", result, $"Transaction isolation level should return to default when reusing a connection from the pool. Isolation level {result}");

            await client.Disconnect();
        }

        [TestMethod]
        public async Task BeginTranUpdate_CloseConnectionWithoutRollbackOrCommit()
        {
            var client = CreatePooledDatabaseClient(pooling: true, minPoolSize: 1, maxPoolSize: 5);
            var cts = new CancellationTokenSource();

            await client.Connect(cts.Token);

            try
            {
                await client.BeginTransaction(cts.Token);

                await client.ExecuteSQLNonQuery("create table temp_test (test varchar(max))", cts.Token);

                // Perform an update within the transaction
                await client.ExecuteSQLNonQuery("insert temp_test values('test')", cts.Token);

                // Close the connection without committing or rolling back the transaction
                await client.Disconnect();
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            // Reconnect and check if the transaction was rolled back
            await client.Connect(cts.Token);
            var transactionCount = await client.ExecuteSQLSingleColumn<int>("SELECT COUNT(*) FROM sys.dm_tran_active_transactions WHERE transaction_id = @@TRANCOUNT", cts.Token);
            Assert.AreEqual(0, transactionCount, "There should be no active transactions after disconnecting without commit or rollback.");

            await client.Disconnect();
        }

    }
}
