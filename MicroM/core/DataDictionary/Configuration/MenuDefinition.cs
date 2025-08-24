using MicroM.Core;
using MicroM.Extensions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MicroM.DataDictionary.Configuration
{
    /// <summary>
    /// Represents the definition of an application menu and the access granted to related entities.
    /// When a user logs in, the menu definitions are used together with the user's groups to determine
    /// which entities are available. Database objects required to support these menus are created during
    /// application initialization.
    /// </summary>
    public class MenuDefinition
    {
        /// <summary>
        /// Identifier for the menu. Defaults to the name of the implementing class.
        /// </summary>
        public readonly string MenuID;

        /// <summary>
        /// Human‑readable text shown for the menu.
        /// </summary>
        public readonly string MenuDescription;

        /// <summary>
        /// Ordered collection of menu items contained in the menu.
        /// </summary>
        public readonly CustomOrderedDictionary<MenuItemDefinition> MenuItems = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MenuDefinition"/> class.
        /// </summary>
        /// <param name="menuDescription">Description text for the menu.</param>
        public MenuDefinition(string menuDescription)
        {
            MenuID = this.GetType().Name;
            MenuDescription = menuDescription;
            FillMenuItemsDictionary();
        }

        /// <summary>
        /// Populates the <see cref="MenuItems"/> collection using reflected instance members.
        /// </summary>
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
