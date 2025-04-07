create or alter proc mmn_brwMenuItems
        @user_group_id Char(20)
		, @menu_id char(50)
        , @like VarChar(max)
        , @d Char(1)
        as

select  [Menu] = rtrim(b.c_menu_id)
        , [Item] = rtrim(b.c_menu_item_id)
		, [Name] = b.vc_menu_item_name
		, [Path] =b.vc_menu_item_path
		, [Access] = case when a.c_menu_item_id is null then convert(bit,0) else convert(bit,1) end
from    microm_menus_items b
		left join [microm_users_groups_menus] a
		on(a.c_menu_id=b.c_menu_id and a.c_menu_item_id=b.c_menu_item_id and a.c_user_group_id=@user_group_id)
where   not exists (
            select  1
            from    sys_tfLike(@like) l
            where   not 
            (
                isnull(rtrim(b.c_menu_item_id),'') like l.phrase
                or isnull(rtrim(b.vc_menu_item_name),'') like l.phrase
                or isnull(rtrim(b.vc_menu_item_path),'') like l.phrase
            )
        )
order by b.c_menu_id, b.vc_menu_item_path