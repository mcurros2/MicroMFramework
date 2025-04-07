using MicroM.Core;
using MicroM.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static LibraryTest.A_DatabaseClientTests;


namespace LibraryTest
{
    public class DatabaseClientTestsUtil
    {
        public class SPHELPParameters
        {
            public Column<string> objname = new(nameof(objname), sql_type: SqlDbType.NVarChar, size: 1552);
            private CustomOrderedDictionary<ColumnBase> _Columns = new();

            public IEnumerable<ColumnBase> Columns => _Columns.Values;

            public SPHELPParameters()
            {
                _Columns.Add(nameof(objname), objname);
            }
        }

        public async static Task ExecuteTestQueryChannel(string test_id, string query, DataResultSetChannel result, CancellationToken ct)
        {
            var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);

            Debug.Print($"Executing query #{test_id}# {DateTime.Now:O}\n{query}");

            await client.Connect(ct);

            await client.ExecuteSQLChannel($"-- #{test_id}#\n{query}", result, ct);

            await client.Disconnect();

        }

        public async static Task<bool> TestQueryCompleted(string test_id, CancellationToken ct)
        {
            var client = new DatabaseClient(DatabaseConfiguration.Server, DatabaseConfiguration.SystemDatabase, DatabaseConfiguration.user, DatabaseConfiguration.password);

            await client.Connect(ct);
            var result_set = await client.ExecuteSQL($"-- Check query completed #{test_id}#\nselect a.session_id, SUBSTRING(b.text,1,255) from sys.dm_exec_requests a OUTER APPLY sys.dm_exec_sql_text(sql_handle) b where b.text like '-- #{test_id}#%'", ct);
            await client.Disconnect();

            if (result_set.Count > 0) Debug.Print($"TestQueryCompleted found SPID {result_set[0].records[0][0]}");

            return result_set.Count == 0;
        }

    }
}
