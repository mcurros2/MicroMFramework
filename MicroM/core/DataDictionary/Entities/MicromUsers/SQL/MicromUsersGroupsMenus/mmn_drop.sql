create or alter proc mmn_drop
        @user_group_id Char(20)
        , @menu_id Char(50)
        , @menu_item_id Char(50)
        as

begin try

    begin tran

    ;with Hierarchy as
    (
        select  c_menu_id, c_menu_item_id
        from    [microm_menus_items]
        where   c_menu_id = @menu_id
                and c_menu_item_id = @menu_item_id
        union all
        select  mi.c_menu_id, mi.c_menu_item_id
        from    [microm_menus_items] mi
                inner join Hierarchy h on mi.c_parent_menu_id = h.c_menu_id
                                      and mi.c_parent_item_id = h.c_menu_item_id
    )
    delete  [microm_users_groups_menus]
    where   c_user_group_id = @user_group_id
            and exists (select 1 from Hierarchy h
                        where h.c_menu_id = [microm_users_groups_menus].c_menu_id
                              and h.c_menu_item_id = [microm_users_groups_menus].c_menu_item_id)

    commit tran
    select  0, 'OK'

end try
begin catch

    if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;

end catch
