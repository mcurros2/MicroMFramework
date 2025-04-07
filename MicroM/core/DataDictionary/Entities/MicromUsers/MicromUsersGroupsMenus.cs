
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{

    public class MicromUsersGroupsMenusDef : EntityDefinition
    {
        public MicromUsersGroupsMenusDef() : base("mmn", nameof(MicromUsersGroupsMenus)) { }

        public readonly Column<string> c_user_group_id = Column<string>.PK();
        public readonly Column<string> c_menu_id = Column<string>.PK(size: 50);
        public readonly Column<string> c_menu_item_id = Column<string>.PK(size: 50);

        public readonly ViewDefinition mmn_brwStandard = new(nameof(c_user_group_id));
        public readonly ViewDefinition mmn_brwMenuItems = new(nameof(c_user_group_id), nameof(c_menu_id));

        public readonly EntityForeignKey<MicromUsersGroups, MicromUsersGroupsMenus> FKGroups = new();
        public readonly EntityForeignKey<MicromMenusItems, MicromUsersGroupsMenus> FKMenus = new();

    }

    public class MicromUsersGroupsMenus : Entity<MicromUsersGroupsMenusDef>
    {
        public MicromUsersGroupsMenus() : base() { }
        public MicromUsersGroupsMenus(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }


}
