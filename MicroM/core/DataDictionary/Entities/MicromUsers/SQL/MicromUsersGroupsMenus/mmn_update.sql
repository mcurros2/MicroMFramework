create or alter proc mmn_update
        @user_group_id Char(20)
        , @menu_id Char(50)
        , @menu_item_id Char(50)
        , @lu DateTime
        , @webusr VarChar(255)
        as

if (@user_group_id is null or trim(@user_group_id) = '') begin select 11, 'The parameter @user_group_id cannot be null or empty' return end
if (@menu_id is null or trim(@menu_id) = '') begin select 11, 'The parameter @menu_id cannot be null or empty' return end
if (@menu_item_id is null or trim(@menu_item_id) = '') begin select 11, 'The parameter @menu_item_id cannot be null or empty' return end

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()
    declare @parent_menu_id char(50), @parent_item_id char(50)

    begin tran

    select  @cu=dt_lu
    from    [microm_users_groups_menus] with (rowlock, holdlock, updlock)
    where   c_user_group_id = @user_group_id
            and c_menu_id = @menu_id
            and c_menu_item_id = @menu_item_id

    if @cu is null
    begin

        insert  [microm_users_groups_menus]
        values
            (
            @user_group_id
            , @menu_id
            , @menu_item_id
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )

        select  @parent_menu_id = c_parent_menu_id
                , @parent_item_id = c_parent_item_id
        from    [microm_menus_items]
        where   c_menu_id = @menu_id
                and c_menu_item_id = @menu_item_id

        while @parent_menu_id is not null and @parent_item_id is not null
        begin
            select  @cu = dt_lu
            from    [microm_users_groups_menus]
            where   c_user_group_id = @user_group_id
                    and c_menu_id = @parent_menu_id
                    and c_menu_item_id = @parent_item_id

            if @cu is null
            begin
                insert  [microm_users_groups_menus]
                values
                    (
                    @user_group_id
                    , @parent_menu_id
                    , @parent_item_id
                    , @now
                    , @now
                    , @webusr
                    , @webusr
                    , @login
                    , @login
                    )
            end

            select    @parent_menu_id = c_parent_menu_id
                    , @parent_item_id = c_parent_item_id
            from    [microm_menus_items]
            where   c_menu_id = @parent_menu_id
                    and c_menu_item_id = @parent_item_id
        end

        commit tran
        select    0, 'OK'
        return
    end

    commit tran
    select 0, 'OK'

end try
begin catch

    if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;

end catch
