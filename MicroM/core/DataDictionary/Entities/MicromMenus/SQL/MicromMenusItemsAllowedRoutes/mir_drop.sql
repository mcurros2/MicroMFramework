create or alter proc mir_drop
        @menu_id Char(50)
        , @menu_item_id Char(50)
        , @route_id Char(20)
        as

begin try

    declare @now datetime = getdate()

    begin tran

    delete  [microm_menus_items_allowed_routes]
    where   c_menu_id = @menu_id
            and c_menu_item_id = @menu_item_id
            and c_route_id = @route_id

    update  [microm_menus]
    set     dt_last_route_updated = @now
	where   c_menu_id = @menu_id

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