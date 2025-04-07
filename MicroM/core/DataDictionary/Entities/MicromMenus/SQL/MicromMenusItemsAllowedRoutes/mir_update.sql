create or alter proc mir_update
        @menu_id Char(50)
        , @menu_item_id Char(50)
        , @route_id Char(20)
        , @route_path VarChar(2048)
        , @lu DateTime
        , @webusr VarChar(255)
        as

if (@menu_id is null or trim(@menu_id) = '') begin select 11, 'The parameter @menu_id cannot be null or empty' return end
if (@menu_item_id is null or trim(@menu_item_id) = '') begin select 11, 'The parameter @menu_item_id cannot be null or empty' return end

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login(), @result int, @msg varchar(255)

    begin tran

    select  @route_id = null
    select  @route_id=c_route_id
    from    [microm_routes] with (rowlock, holdlock, updlock)
	where   vc_route_path = @route_path

    if(@route_id is null)
    begin
        set @result=-1
        exec mro_iupdate @route_id, @route_path, null, @webusr, @result output, @msg output

        if @result not in (0, 15)
        begin
			commit tran
			select @result, @msg
			return
		end

        select  @route_id=@msg
	end

    select  @cu=dt_lu
    from    [microm_menus_items_allowed_routes] with (rowlock, holdlock, updlock)
    where   c_menu_id = @menu_id
            and c_menu_item_id = @menu_item_id
            and c_route_id = @route_id

    if @cu is null
    begin

        insert  [microm_menus_items_allowed_routes]
        values
            (
            @menu_id
            , @menu_item_id
            , @route_id
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )

        update  [microm_menus]
        set     dt_last_route_updated = @now
		where   c_menu_id = @menu_id

        commit tran
        select    15, @route_id
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