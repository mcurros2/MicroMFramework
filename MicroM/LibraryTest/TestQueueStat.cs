using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.Web.Services;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryTest
{

    public class TestQueueStatusDef : EntityDefinition
    {


        public TestQueueStatusDef() : base("queus", nameof(TestQueueStatus)) { }

        public readonly Column<string> c_queue_id = Column<string>.PK();
        public readonly Column<string> c_status_id = Column<string>.PK();
        public readonly Column<string> c_statusvalue_id = Column<string>.FK();


        public ViewDefinition queus_brwStandard { get; private set; }
        protected override void DefineViews()
        {
            // MMC: Setting BrowsingKeyPamr is not needed.
            // if this is empty it will map the last key column in ds_column_mappings
            queus_brwStandard = new ViewDefinition(true, new ViewParm(c_queue_id, column_mapping: 0, browsing_key: true));

        }

        public readonly EntityForeignKey<TestQueue, TestQueueStatus> FKQueue = new();
        public readonly EntityForeignKey<StatusValues, TestQueueStatus> FKCategoriesValues = new();

    }


    public class TestQueueStatus : Entity<TestQueueStatusDef>
    {

        public TestQueueStatus() : base() { }

        public TestQueueStatus(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

        public async static Task<TestQueueStatus> CreateAndGet(IEntityClient ec, string queue_id, string status_id, CancellationToken ct)
        {
            var ret = new TestQueueStatus(ec);
            ret.Def.c_queue_id.Value = queue_id;
            ret.Def.c_status_id.Value = status_id;
            await ret.Data.GetData(ct);
            return ret;
        }


    }

}
