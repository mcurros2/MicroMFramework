
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{

    public class MicromMenusItemsDef : EntityDefinition
    {
        public MicromMenusItemsDef() : base("mmi", nameof(MicromMenusItems)) { }

        public readonly Column<string> c_menu_id = Column<string>.PK(size: 50);
        public readonly Column<string> c_menu_item_id = Column<string>.PK(size: 50);

        public readonly Column<string?> c_parent_menu_id = Column<string?>.FK(size: 50, nullable: true);
        public readonly Column<string?> c_parent_item_id = Column<string?>.FK(size: 50, nullable: true);


        public readonly Column<string> vc_menu_item_path = Column<string>.Text(size: 0);
        public readonly Column<string> vc_menu_item_name = Column<string>.Text();

        public readonly ViewDefinition mmi_brwStandard = new(nameof(c_menu_id));


        public readonly EntityForeignKey<MicromMenus, MicromMenusItems> FKMenus = new();

        public readonly EntityForeignKey<MicromMenusItems, MicromMenusItems> FKParent = new(
            key_mappings: [
                new(parentColName: nameof(c_menu_id), childColName: nameof(c_parent_menu_id)),
                new(parentColName: nameof(c_menu_item_id), childColName: nameof(c_parent_item_id))
                ]);

    }

    public class MicromMenusItems : Entity<MicromMenusItemsDef>
    {
        public MicromMenusItems() : base() { }
        public MicromMenusItems(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }


}
