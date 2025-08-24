namespace MicroM.DataDictionary.Configuration
{
    /// <summary>
    /// Defines a single item in an application menu.
    /// </summary>
    /// <param name="menuItemDescription">Display text for the menu item.</param>
    /// <param name="parentMenuItemId">Optional identifier of the parent menu item.</param>
    /// <param name="allowed_routes">Optional list of routes permitted for this menu item.</param>
    public class MenuItemDefinition(string menuItemDescription, string? parentMenuItemId = null, List<string>? allowed_routes = null)
    {
        /// <summary>
        /// Gets the identifier of the menu to which this item belongs.
        /// </summary>
        public string MenuID { get; internal set; } = "";

        /// <summary>
        /// Gets the identifier assigned to this menu item.
        /// </summary>
        public string MenuItemID { get; internal set; } = "";

        /// <summary>
        /// Optional identifier of the parent menu item.
        /// </summary>
        public string? ParentMenuItemID = parentMenuItemId;

        /// <summary>
        /// Gets the description displayed for the menu item.
        /// </summary>
        public string MenuItemDescription = menuItemDescription;

        /// <summary>
        /// Gets the child menu items.
        /// </summary>
        public readonly List<MenuItemDefinition> Children = [];

        /// <summary>
        /// Gets or sets the parent menu item reference.
        /// </summary>
        public MenuItemDefinition? Parent { get; internal set; }

        /// <summary>
        /// Gets the list of routes permitted for this menu item.
        /// </summary>
        public readonly List<string>? AllowedRoutes = allowed_routes;

        /// <summary>
        /// Gets the resolved item path within the menu hierarchy.
        /// </summary>
        public string ItemPath { get; internal set; } = "";
    }
}
