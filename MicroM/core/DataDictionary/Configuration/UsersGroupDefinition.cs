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
        public readonly string GroupID;
        public readonly string GroupDescription;

        public readonly Dictionary<string, MenuItemDefinition> AllowedMenuItems = new(StringComparer.OrdinalIgnoreCase);

        public UsersGroupDefinition(string menuDescription)
        {
            GroupID = this.GetType().Name;
            GroupDescription = menuDescription;
        }

        public void AddMenuItem(MenuItemDefinition menuItem)
        {
            if(!AllowedMenuItems.TryAdd($"{menuItem.MenuID}.{menuItem.MenuItemID}", menuItem))
            {
                throw new ArgumentException($"Menu item already exists: {menuItem.MenuItemDescription}");
            }
        }

        public void AddMenuAllItems(MenuDefinition menu)
        {
            foreach (var item in menu.MenuItems)
            {
                AddMenuItem(item);
            }
        }

    }
}
