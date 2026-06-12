using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.Entities;
using MicroM.Web.Services;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryTest;

public class TestQueueCatDef : EntityDefinition
{


    public TestQueueCatDef() : base("queuc", nameof(TestQueueCat)) { }

    public readonly Column<string> c_queue_id = Column<string>.PK();
    public readonly Column<string> c_category_id = Column<string>.PK();
    public readonly Column<string> c_categoryvalue_id = Column<string>.FK();


    public ViewDefinition queuc_brwStandard { get; private set; }
    protected override void DefineViews()
    {
        // MMC: Setting BrowsingKeyPamr is not needed.
        // if this is empty it will map the last key column in ds_column_mappings
        queuc_brwStandard = new ViewDefinition(true, new ViewParm(c_queue_id, column_mapping: 0, browsing_key: true));

    }

    public ProcedureDefinition queuc_lookup { get; private set; } = new(nameof(c_queue_id));

    public readonly EntityForeignKey<TestQueue, TestQueueCat> FKQueue = new();
    public readonly EntityForeignKey<CategoriesValues, TestQueueCat> FKCategoriesValues = new();

}


public class TestQueueCat : Entity<TestQueueCatDef>
{

    public TestQueueCat() : base() { }

    public TestQueueCat(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

    public async static Task<TestQueueCat> CreateAndGet(IEntityClient ec, string queue_id, string category_id, CancellationToken ct, string? schema_name = null)
    {
        var ret = new TestQueueCat(ec, schema_name: schema_name);
        ret.Def.c_queue_id.Value = queue_id;
        ret.Def.c_category_id.Value = category_id;
        await ret.Data.GetData(ct);
        return ret;
    }


}
