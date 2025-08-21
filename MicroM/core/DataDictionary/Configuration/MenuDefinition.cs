using MicroM.Core;
using MicroM.Extensions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MicroM.DataDictionary.Configuration
{
    /// <summary>
    /// This class is used to define an application menu. It also defines the access granted to the entities related to the menu.
    /// When a user logs in, the system will check the user's groups and the menu definitions to determine which entities the user can access.
    /// <see cref="MenuItemDefinition"/>, <see cref="MicromUsersGroups"/>
    /// When creating an application database the system will create the necessary tables and views to support the menu definitions.
    /// </summary>
    public class MenuDefinition
    {
        /// <summary>
        /// Gets the identifier of the menu. Defaults to the class name.
        /// </summary>
        public readonly string MenuID;

        /// <summary>
        /// Gets the description displayed for the menu.
        /// </summary>
        public readonly string MenuDescription;

        /// <summary>
        /// Gets the ordered collection of menu items contained in the menu.
        /// </summary>
        public readonly CustomOrderedDictionary<MenuItemDefinition> MenuItems = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MenuDefinition"/> class with the provided description.
        /// </summary>
        /// <param name="menuDescription">Text describing the menu.</param>
        public MenuDefinition(string menuDescription)
        {
            MenuID = this.GetType().Name;
            MenuDescription = menuDescription;
            FillMenuItemsDictionary();
        }

        private void FillMenuItemsDictionary()
        {
            IOrderedEnumerable<MemberInfo> instance_members = this.GetType().GetAndCacheInstanceMembers();

            foreach (var prop in instance_members)
            {
                if (prop.MemberType.IsIn(MemberTypes.Property, MemberTypes.Field) && prop.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                {
                    if (prop.GetMemberType() == typeof(MenuItemDefinition))
                    {
                        var menu_item = (MenuItemDefinition?)prop.GetMemberValue(this);
                        if (menu_item != null)
                        {
                            if (MenuItems.TryAdd(prop.Name, menu_item))
                            {
                                menu_item.MenuID = this.MenuID;
                                menu_item.MenuItemID = prop.Name;
                                if (menu_item.ParentMenuItemID != null)
                                {
                                    if (MenuItems.TryGetValue(menu_item.ParentMenuItemID, out MenuItemDefinition? parent))
                                    {
                                        menu_item.Parent = parent;
                                        parent!.Children.Add(menu_item);
                                        menu_item.ItemPath = $"{parent.ItemPath}/{menu_item.MenuItemID}";
                                    }
                                    else
                                    {
                                        throw new ArgumentException($"Parent MenuItem not found: Value {menu_item.ParentMenuItemID} ({menu_item.MenuItemDescription}), Menu {this.MenuID} ({this.MenuDescription})");
                                    }
                                }
                                else
                                {
                                    menu_item.ItemPath = $"/{menu_item.MenuItemID}";
                                }
                            }
                            else
                            {
                                throw new ArgumentException($"Duplicate MenuItem: Value {menu_item.MenuItemID} ({menu_item.MenuItemDescription}), Menu {this.MenuID} ({this.MenuDescription})");
                            }
                        }
                    }
                }
            }
        }
    }
}
