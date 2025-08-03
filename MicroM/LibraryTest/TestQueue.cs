#nullable enable
using LibraryTest.DataDictionary;
using LibraryTest.DataDictionary.CategoriesData;
using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;


namespace LibraryTest
{

    public class TestQueueDef : EntityDefinition
    {


        public TestQueueDef() : base("queu", nameof(TestQueue)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

        public readonly Column<string> c_queue_id = Column<string>.PK();
        public readonly Column<string> vc_description = new(sql_type: SqlDbType.VarChar, size: 255);
        public readonly Column<string?> c_queuetype_id = Column<string?>.EmbedCategory(nameof(QueueType))!;
        public readonly Column<string?> c_queuestatus_id = Column<string?>.EmbedStatus(nameof(QUEUE))!;
        public readonly Column<DateOnly> dt_init = new();


        public ViewDefinition queu_brwStandard { get; private set; }

        protected override void DefineViews()
        {
            // MMC: Setting BrowsingKeyPamr is not needed.
            // if this is empty it will map the last key column in ds_column_mappings
            queu_brwStandard = new ViewDefinition(true, new ViewParm(c_queue_id, column_mapping: 0, browsing_key: true));

        }

        public ProcedureDefinition queu_lookup { get; private set; } = new(readonly_locks: true, nameof(c_queue_id));
        public readonly ProcedureDefinition queu_testParameterless = new();

        public ACTTestAction ACTTest { get; private set; } = new();

    }


    public class TestQueue : Entity<TestQueueDef>
    {

        public TestQueue() : base() { }

        public TestQueue(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

        public async static Task<TestQueue> CreateAndGet(IEntityClient ec, string queue_id, CancellationToken ct)
        {
            var ret = new TestQueue(ec);
            ret.Def.c_queue_id.Value = queue_id;
            await ret.Data.GetData(ct);
            return ret;
        }


    }

    public record TestQueueActionResult : EntityActionResult
    {
        public string TestResult { get; set; }
        public string AdminUser { get; set; }
        public string AdminPassword { get; set; }
    }

    public class ACTTestAction : EntityActionBase
    {
        public override Task<EntityActionResult> Execute(EntityBase entity, DataWebAPIRequest parms, EntityDefinition def, MicroMOptions? Options, IWebAPIServices? API, IMicroMEncryption? encryptor, CancellationToken ct, string? app_id)
        {
            if (Options == null || parms.ServerClaims == null) throw new ArgumentNullException();

            TestQueueActionResult result = new()
            {
                AdminUser = (string)parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername],
                AdminPassword = encryptor?.Decrypt((string)parms.ServerClaims[MicroMServerClaimTypes.MicroMPassword]) ?? (string)parms.ServerClaims[MicroMServerClaimTypes.MicroMPassword],
                TestResult = "OK"
            };

            return Task.FromResult(result as EntityActionResult);
        }
    }

}
