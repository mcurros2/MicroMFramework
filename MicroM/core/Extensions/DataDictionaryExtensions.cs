using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.DataDictionary.Configuration;

namespace MicroM.Extensions;

public static class DataDictionaryExtensions
{
    /// <summary>
    /// Inserts into <see cref="ObjectsStatus"/> Data Dictionay each <see cref="Status"/> in <see cref="EntityDefinition.RelatedStatus"/> for the <see cref="Entity{TDefinition}"/>.
    /// This enables the use of the status by the entity and inserts its initial status value when creating a new record.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <param name="ec"></param>
    /// <param name="status_id"></param>
    /// <param name="object_id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async static Task AddStatusRelations(this EntityBase entity, IEntityClient ec, CancellationToken ct)
    {
        if (entity.Def.RelatedStatus.Count == 0) return;

        var ost = new ObjectsStatus(ec);
        foreach (var status_id in entity.Def.RelatedStatus)
        {
            ost.Def.c_status_id.Value = status_id;
            ost.Def.c_object_id.Value = entity.Def.Mneo;
            await ost.InsertData(ct);
        }

    }

    /// <summary>
    /// Inserts into <see cref="ObjectsCategories"/> Data Dictionay each <see cref="Categories"/> in <see cref="EntityDefinition.RelatedCategories"/> for the <see cref="Entity{TDefinition}"/>.
    /// Relates a category in the data dictionary to the specified entity.
    /// This enables the use of the category by the entity.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <param name="ec"></param>
    /// <param name="category_id"></param>
    /// <param name="object_id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async static Task AddCategoryRelations(this EntityBase entity, IEntityClient ec, CancellationToken ct)
    {
        if (entity.Def.RelatedCategories.Count == 0) return;

        var oca = new ObjectsCategories(ec);
        foreach (var category_id in entity.Def.RelatedCategories)
        {
            oca.Def.c_category_id.Value = category_id;
            oca.Def.c_object_id.Value = entity.Def.Mneo;
            await oca.InsertData(ct);

        }
    }

    /// <summary>
    /// Inserts a value in the database to an existing category in the data dictionary
    /// </summary>
    /// <param name="cat"></param>
    /// <param name="ec"></param>
    /// <param name="categoryvalue_id"></param>
    /// <param name="description"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async static Task<CategoriesValues> AddCategoryValue(this Categories cat, IEntityClient ec, string categoryvalue_id, string description, CancellationToken ct)
    {
        var cav = new CategoriesValues(ec);
        cav.Def.c_category_id.Value = cat.Def.c_category_id.Value;
        cav.Def.c_categoryvalue_id.Value = categoryvalue_id;
        cav.Def.vc_description.Value = description;
        await cav.InsertData(ct);
        await cav.GetData(ct);
        return cav;
    }

    /// <summary>
    /// Inserts a value in the database to an existing status in the Data Dictionary
    /// </summary>
    /// <param name="stat"></param>
    /// <param name="ec"></param>
    /// <param name="statusvalue_id"></param>
    /// <param name="description"></param>
    /// <param name="init_value"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async static Task<StatusValues> AddStatusValue(this Status stat, IEntityClient ec, string statusvalue_id, string description, bool init_value, CancellationToken ct)
    {
        var stv = new StatusValues(ec);
        stv.Def.c_status_id.Value = stat.Def.c_status_id.Value;
        stv.Def.c_statusvalue_id.Value = statusvalue_id;
        stv.Def.vc_description.Value = description;
        stv.Def.bt_initial_value.Value = init_value;
        await stv.InsertData(ct);
        await stv.GetData(ct);
        return stv;
    }

    /// <summary>
    /// Add a status record to the status and status_values data dictionary table
    /// </summary>
    /// <param name="stc"></param>
    /// <param name="ec"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async static Task<Status> AddStatus(this StatusDefinition stc, IEntityClient ec, CancellationToken ct)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);

        var stat = new Status(ec);
        stat.Def.c_status_id.Value = stc.StatusID;
        stat.Def.vc_description.Value = stc.Description;

        try
        {
            await stat.InsertData(ct);
            await stat.GetData(ct);

            var status_values = stc.GetPropertiesOrFields<StatusValuesDefinition, StatusDefinition>();
            foreach (var stv in status_values)
            {
                await stat.AddStatusValue(ec, stv.StatusValueID, stv.Description, stv.InitialValue, ct);
            }
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }

        return stat;
    }


    /// <summary>
    /// Add a category record to the categories and categories_values data dictionary table
    /// </summary>
    /// <param name="cac"></param>
    /// <param name="ec"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async static Task<Categories> AddCategory(this CategoryDefinition cac, IEntityClient ec, CancellationToken ct)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);

        var cat = new Categories(ec);
        cat.Def.c_category_id.Value = cac.CategoryID;
        cat.Def.vc_description.Value = cac.Description;

        try
        {
            await cat.InsertData(ct);
            await cat.GetData(ct);

            var cat_values = cac.GetPropertiesOrFields<CategoryValuesDefinition, CategoryDefinition>();
            foreach (var cav in cat_values)
            {
                await cat.AddCategoryValue(ec, cav.CategoryValueID, cav.Description, ct);
            }

        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }

        return cat;
    }


    public static async Task<T> AddToDataDictionary<T>(this T ent, CancellationToken ct) where T : EntityBase, new()
    {
        return (T)await AddInstanceToDataDictionary(ent, ct);
    }

    public static async Task<EntityBase> AddInstanceToDataDictionary(this EntityBase ent, CancellationToken ct)
    {
        var ec = ent.Client;

        // MMC: create the object in Data Dictionary
        Objects obj = new(ec);
        obj.Def.c_object_id.Value = ent.Def.Mneo;
        obj.Def.c_mneo_id.Value = ent.Def.Mneo;
        obj.Def.vc_tablename.Value = ent.Def.TableName;
        await obj.InsertData(ct);

        // MMC: create autonum in numbering table
        if (ent.Def.AutonumColumn != null)
        {
            Numbering num = new(ec);
            num.Def.c_object_id.Value = ent.Def.Mneo;
            num.Def.bi_lastnumber.Value = 0;
            await num.InsertData(ct);
        }

        await ent.AddCategoryRelations(ec, ct);
        await ent.AddStatusRelations(ec, ct);

        return ent;
    }

    public static async Task AddMenu(this MenuDefinition menu_definition, IEntityClient ec, CancellationToken ct)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);

        try
        {
            await ec.Connect(ct);

            var menu = new MicromMenus(ec);
            menu.Def.c_menu_id.Value = menu_definition.MenuID;
            menu.Def.vc_menu_name.Value = menu_definition.MenuDescription;

            await menu.InsertData(ct);

            var menu_item = new MicromMenusItems(ec);
            foreach (var item in menu_definition.MenuItems)
            {
                if (item != null)
                {
                    menu_item.Def.c_menu_id.Value = menu_definition.MenuID;
                    menu_item.Def.c_menu_item_id.Value = item.MenuItemID;

                    if (item.ParentMenuItemID != null)
                    {
                        menu_item.Def.c_parent_menu_id.Value = menu_definition.MenuID;
                        menu_item.Def.c_parent_item_id.Value = item.ParentMenuItemID;
                    }
                    else
                    {
                        menu_item.Def.c_parent_menu_id.Value = null;
                        menu_item.Def.c_parent_item_id.Value = null;
                    }

                    menu_item.Def.vc_menu_item_name.Value = item.MenuItemDescription;
                    menu_item.Def.vc_menu_item_path.Value = item.ItemPath;
                    await menu_item.InsertData(ct);

                    if (item.AllowedRoutes != null)
                    {
                        var allowed = new MicromMenusItemsAllowedRoutes(ec);
                        foreach (var route in item.AllowedRoutes)
                        {
                            allowed.Def.c_menu_id.Value = menu_definition.MenuID;
                            allowed.Def.c_menu_item_id.Value = item.MenuItemID;
                            allowed.Def.vc_route_path.Value = route;
                            await allowed.InsertData(ct);
                        }
                    }

                }
            }
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
    }

    public static async Task AddUserGroup(this UsersGroupDefinition user_group, IEntityClient ec, CancellationToken ct)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        try
        {
            await ec.Connect(ct);

            var ug = new MicromUsersGroups(ec);
            ug.Def.c_user_group_id.Value = user_group.GroupID;
            ug.Def.vc_user_group_name.Value = user_group.GroupDescription;
            await ug.InsertData(ct);

            var ug_menu = new MicromUsersGroupsMenus(ec);
            foreach (var menu in user_group.AllowedMenuItems.Values)
            {
                ug_menu.Def.c_user_group_id.Value = user_group.GroupID;
                ug_menu.Def.c_menu_id.Value = menu.MenuID;
                ug_menu.Def.c_menu_item_id.Value = menu.MenuItemID;
                await ug_menu.InsertData(ct);
            }
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
    }


}
