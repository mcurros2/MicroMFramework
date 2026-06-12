using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace LibraryTest;

public class TestFileDef : EntityDefinition
{
    public TestFileDef() : base("tfi", nameof(TestFile))
    {
        SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop;
    }

    public readonly Column<string> c_testfile_id = Column<string>.PK();
    public readonly Column<string> vc_filename = Column<string>.Text(size: 255);
    public readonly Column<Stream> vb_content = Column<Stream>.BinaryStream(nullable: false);
}

public class TestFile : Entity<TestFileDef>
{
    public TestFile() : base() { }
    public TestFile(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name: schema_name) { }

    public static async Task<TestFile> CreateAndGet(IEntityClient ec, string id, CancellationToken ct, string? schema_name = null)
    {
        var ret = new TestFile(ec, schema_name: schema_name);
        ret.Def.c_testfile_id.Value = id;
        await ret.Data.GetData(ct);
        return ret;
    }
}