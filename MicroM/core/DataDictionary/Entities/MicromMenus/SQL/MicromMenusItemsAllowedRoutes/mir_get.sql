create or alter proc mir_get
        @menu_id Char(50)
        , @menu_item_id Char(50)
        , @route_id Char(20)
        as

select  [c_menu_id] = rtrim(a.c_menu_id)
        , [c_menu_item_id] = rtrim(a.c_menu_item_id)
        , [c_route_id] = rtrim(a.c_route_id)
        , b.vc_route_path /* fake column vc_route_path */
        , a.dt_inserttime
        , a.dt_lu
        , a.vc_webinsuser
        , a.vc_webluuser
        , a.vc_insuser
        , a.vc_luuser
from    [microm_menus_items_allowed_routes] a
        join microm_routes b
        on(a.c_route_id = b.c_route_id)
where   a.c_menu_id = @menu_id
        and a.c_menu_item_id = @menu_item_id
        and a.c_route_id = @route_id