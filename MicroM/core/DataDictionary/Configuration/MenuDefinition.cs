using MicroM.Core;
using MicroM.DataDictionary.Entities;
using MicroM.Extensions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MicroM.DataDictionary.Configuration;

/// <summary>
/// This class is used to define an application menu. It also defines the access granted to the entities related to the menu.
/// When a user logs in, the system will check the user's groups and the menu definitions to determine which entities the user can access.
/// <see cref="MenuItemDefinition"/>, <see cref="MicromUsersGroups"/>
/// When creating an application database the system will create the necessary tables and views to support the menu definitions.
/// </summary>
public class MenuDefinition
{
    public readonly string MenuID;
    public readonly string MenuDescription;

    public readonly CustomOrderedDictionary<MenuItemDefinition> MenuItems = new();

    public MenuDefinition(string menuDescription)
    {
        MenuID = this.GetType().Name;
        MenuDescription = menuDescription;
        FillMenuItemsDictionary();
    }

    private void FillMenuItemsDictionary()
    {
        var visitedMenuItems = new HashSet<MenuItemDefinition>();

        ProcessContainer(this, defaultParentMenuItemID: null);

        void ProcessContainer(object container, string? defaultParentMenuItemID)
        {
            IOrderedEnumerable<MemberInfo> instance_members = container.GetType().GetAndCacheInstanceMembers();

            foreach (var member in instance_members)
            {
                if (!member.MemberType.IsIn(MemberTypes.Property, MemberTypes.Field) ||
                    member.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
                {
                    continue;
                }

                if (member.GetMemberType() != typeof(MenuItemDefinition))
                {
                    continue;
                }

                // Skip base infrastructure members like MenuItemDefinition.Parent
                if (member.DeclaringType == typeof(MenuItemDefinition))
                {
                    continue;
                }

                var menu_item = (MenuItemDefinition?)member.GetMemberValue(container);
                if (menu_item == null)
                {
                    continue;
                }

                if (menu_item.ParentMenuItemID == null && defaultParentMenuItemID != null)
                {
                    menu_item.ParentMenuItemID = defaultParentMenuItemID;
                }

                AddMenuItem(member.Name, menu_item);

                if (visitedMenuItems.Add(menu_item))
                {
                    ProcessContainer(menu_item, member.Name);
                }
            }
        }

        void AddMenuItem(string menuItemID, MenuItemDefinition menu_item)
        {
            if (!MenuItems.TryAdd(menuItemID, menu_item))
            {
                throw new ArgumentException($"Duplicate MenuItem: Value {menuItemID} ({menu_item.MenuItemDescription}), Menu {this.MenuID} ({this.MenuDescription})");
            }

            menu_item.MenuID = this.MenuID;
            menu_item.MenuItemID = menuItemID;

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
    }
}
