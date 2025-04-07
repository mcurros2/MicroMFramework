create or alter proc mug_GetAllGroupsAllowedRoutes
		as

select	distinct
		c_user_group_id = rtrim(a.c_user_group_id)
		, c.vc_route_path
		, c_route_id = rtrim(c.c_route_id)
		, e.dt_last_route_updated
from	microm_users_groups_menus a
		join microm_menus_items_allowed_routes b
		on(b.c_menu_id=a.c_menu_id and b.c_menu_item_id=a.c_menu_item_id)
		join microm_routes c
		on(b.c_route_id=c.c_route_id)
		join microm_menus_items d
		on(d.c_menu_id=b.c_menu_id and d.c_menu_item_id=b.c_menu_item_id)
		join microm_menus e
		on(e.c_menu_id=d.c_menu_id)