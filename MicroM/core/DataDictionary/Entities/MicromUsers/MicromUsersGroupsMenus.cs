using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Schema definition mapping groups to menu permissions.
    /// </summary>
    public class MicromUsersGroupsMenusDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicromUsersGroupsMenusDef"/> class.
        /// </summary>
        public MicromUsersGroupsMenusDef() : base("mmn", nameof(MicromUsersGroupsMenus)) { }

        /// <summary>User group identifier.</summary>
        public readonly Column<string> c_user_group_id = Column<string>.PK();
        /// <summary>Menu identifier.</summary>
        public readonly Column<string> c_menu_id = Column<string>.PK(size: 50);
        /// <summary>Menu item identifier.</summary>
        public readonly Column<string> c_menu_item_id = Column<string>.PK(size: 50);

        /// <summary>View listing groups associated with menus.</summary>
        public readonly ViewDefinition mmn_brwStandard = new(nameof(c_user_group_id));
        /// <summary>View listing menu items for a group.</summary>
        public readonly ViewDefinition mmn_brwMenuItems = new(nameof(c_user_group_id), nameof(c_menu_id));

        /// <summary>Relationship to groups.</summary>
        public readonly EntityForeignKey<MicromUsersGroups, MicromUsersGroupsMenus> FKGroups = new();
        /// <summary>Relationship to menu items.</summary>
        public readonly EntityForeignKey<MicromMenusItems, MicromUsersGroupsMenus> FKMenus = new();

    }

    /// <summary>
    /// Entity for managing group-to-menu assignments.
    /// </summary>
    public class MicromUsersGroupsMenus : Entity<MicromUsersGroupsMenusDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicromUsersGroupsMenus"/> class.
        /// </summary>
        public MicromUsersGroupsMenus() : base() { }
        /// <summary>
        /// Initializes a new instance with a database client and optional encryptor.
        /// </summary>
        /// <param name="ec">Entity client.</param>
        /// <param name="encryptor">Optional encryptor.</param>
        public MicromUsersGroupsMenus(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }

}

