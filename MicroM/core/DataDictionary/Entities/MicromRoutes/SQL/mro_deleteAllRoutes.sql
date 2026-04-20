create or alter proc [dbo].mro_deleteAllRoutes as
begin try

	begin tran

		delete [dbo].[microm_menus_items_allowed_routes]
		delete [dbo].[microm_routes]

	commit tran

	select	0, 'OK'
end try
begin catch

	if @@trancount > 0	rollback tran
	throw;

end catch