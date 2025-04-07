using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary
{
    public class CategoriesDef : EntityDefinition
    {
        public CategoriesDef() : base("cat", nameof(Categories)) { }

        public readonly Column<string> c_category_id = Column<string>.PK();
        public readonly Column<string> vc_description = new(sql_type: SqlDbType.VarChar, size: 255);

        public ViewDefinition cat_brwStandard { get; private set; } = new(nameof(c_category_id));

    }

    public class Categories : Entity<CategoriesDef>
    {
        public Categories() : base() { }
        public Categories(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }

}
