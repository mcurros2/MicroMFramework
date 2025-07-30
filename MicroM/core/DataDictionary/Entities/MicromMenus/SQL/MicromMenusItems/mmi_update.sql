create or alter proc mmi_update
        @menu_id Char(50)
        , @menu_item_id Char(50)
        , @parent_menu_id Char(50)
        , @parent_item_id Char(50)
        , @menu_item_path VarChar(max)
        , @menu_item_name VarChar(255)
        , @lu DateTime
        , @webusr VarChar(255)
        as

if (@menu_id is null or trim(@menu_id) = '') begin select 11, 'The parameter @menu_id cannot be null or empty' return end
if (@menu_item_id is null or trim(@menu_item_id) = '') begin select 11, 'The parameter @menu_item_id cannot be null or empty' return end

set @parent_menu_id = NULLIF(@parent_menu_id,'')
set @parent_item_id = NULLIF(@parent_item_id,'')

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    begin tran

    select  @cu=dt_lu
    from    [microm_menus_items] with (rowlock, holdlock, updlock)
    where   c_menu_id = @menu_id
            and c_menu_item_id = @menu_item_id

    if @cu is null
    begin

        if exists (select 1 from [microm_menus_items] where c_menu_id = @menu_id and vc_menu_item_path = @menu_item_path)
        begin
            rollback tran
            select 11, 'The menu item path already exists. '+rtrim(@menu_id)+' '+@menu_item_path
            return
        end

        insert  [microm_menus_items]
        values
            (
            @menu_id
            , @menu_item_id
            , @parent_menu_id
            , @parent_item_id
            , @menu_item_path
            , @menu_item_name
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )

        commit tran
        select    0, 'OK'
        return
    end

    if @cu<>@lu or @lu is null 
    begin
        commit tran
        select 4, 'Record changed'
        return
    end

    update  [microm_menus_items]
    set     c_parent_menu_id = @parent_menu_id
            , c_parent_item_id = @parent_item_id
            , vc_menu_item_path = @menu_item_path
            , vc_menu_item_name = @menu_item_name
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_menu_id = @menu_id
            and c_menu_item_id = @menu_item_id

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