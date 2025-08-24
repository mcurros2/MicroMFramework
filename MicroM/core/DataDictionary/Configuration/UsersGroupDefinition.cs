namespace MicroM.DataDictionary.Configuration
{
    /// <summary>
    /// This class is used to define a user group.
    /// When a user logs in, the system will check the user's groups and the menu definitions to determine which entities the user can access.
    /// <see cref="MenuItemDefinition"/>, <see cref="MicromUsersGroups"/>
    /// When creating an application database the system will create the necessary tables and views to support the menu definitions.
    /// </summary>
    public class UsersGroupDefinition
    {
        /// <summary>
        /// Gets the identifier of the group. Defaults to the class name.
        /// </summary>
        public readonly string GroupID;

        /// <summary>
        /// Gets the description for the group.
        /// </summary>
        public readonly string GroupDescription;

        /// <summary>
        /// Gets the dictionary of menu items allowed for the group keyed by menu and item identifier.
        /// </summary>
        public readonly Dictionary<string, MenuItemDefinition> AllowedMenuItems = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersGroupDefinition"/> class with the provided description.
        /// </summary>
        /// <param name="menuDescription">Description of the group.</param>
        public UsersGroupDefinition(string menuDescription)
        {
            GroupID = this.GetType().Name;
            GroupDescription = menuDescription;
        }

        /// <summary>
        /// Adds a single menu item to the list of allowed items.
        /// </summary>
        /// <param name="menuItem">Menu item definition to allow.</param>
        public void AddMenuItem(MenuItemDefinition menuItem)
        {
            if(!AllowedMenuItems.TryAdd($"{menuItem.MenuID}.{menuItem.MenuItemID}", menuItem))
            {
                throw new ArgumentException($"Menu item already exists: {menuItem.MenuItemDescription}");
            }
        }

        /// <summary>
        /// Adds all menu items from a menu definition to the allowed list.
        /// </summary>
        /// <param name="menu">Menu definition containing items to allow.</param>
        public void AddMenuAllItems(MenuDefinition menu)
        {
            foreach (var item in menu.MenuItems)
            {
                AddMenuItem(item);
            }
        }

    }
}
