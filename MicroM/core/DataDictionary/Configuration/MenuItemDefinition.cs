namespace MicroM.DataDictionary.Configuration
{

    public class MenuItemDefinition(string menuItemDescription, string? parentMenuItemId = null, List<string>? allowed_routes = null)
    {
        public string MenuID { get; internal set; } = "";
        public string MenuItemID { get; internal set; } = "";

        public string? ParentMenuItemID = parentMenuItemId;

        public string MenuItemDescription = menuItemDescription;

        public readonly List<MenuItemDefinition> Children = [];

        public MenuItemDefinition? Parent { get; internal set; }

        public readonly List<string>? AllowedRoutes = allowed_routes;

        public string ItemPath { get; internal set; } = "";
    }
}
