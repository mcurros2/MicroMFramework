using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Web.Services;
using System.Data;

namespace LibraryTest
{

    public class TestQueueItemsDef : EntityDefinition
    {


        public TestQueueItemsDef() : base("tqit", nameof(TestQueueItems)) { }


        public readonly Column<string> c_queue_id = Column<string>.PK();
        public readonly Column<string> c_queueitem_id = Column<string>.PK();
        public readonly Column<string> vc_descripcion = new(sql_type: SqlDbType.VarChar, size: 255);


        public ViewDefinition tqit_brwStandard { get; private set; }
        protected override void DefineViews()
        {
            tqit_brwStandard = new(true,
                c_queue_id.AsViewItemParm(column_mapping: -1, compound_group: "1", compound_position: 0),
                c_queueitem_id.AsViewItemParm(column_mapping: 0, compound_group: "1", compound_position: 1, browsing_key: true)
                );

        }


        public ProcedureDefinition tqit_lookup { get; private set; } = new(readonly_locks: true, nameof(c_queue_id), nameof(c_queueitem_id));
        public ProcedureDefinition tqit_testProc { get; private set; }
        protected override void DefineProcs()
        {

            tqit_lookup = new(parms: new ColumnBase[] { c_queue_id, c_queueitem_id }, readonly_locks: true);

            tqit_testProc = new(c_queue_id, c_queueitem_id);
            Column<string> test_output = tqit_testProc.AddParm<string>(nameof(test_output), SqlDbType.VarChar, size: 255, output: true);

        }

        public EntityForeignKey<TestQueue, TestQueueItems> FK_tque_tqit { get; private set; } = new();

        public EntityLookup LKP_tque_lookup { get; private set; }

        protected override void DefineConstraints()
        {
            LKP_tque_lookup = FK_tque_tqit.AddLookup(nameof(TestQueueDef.queu_brwStandard), nameof(TestQueueDef.queu_lookup), 0 , 1);
        }

    }

    public class TestQueueItems : Entity<TestQueueItemsDef>
    {
        public TestQueueItems() : base() { }

        public TestQueueItems(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }


    }
}
