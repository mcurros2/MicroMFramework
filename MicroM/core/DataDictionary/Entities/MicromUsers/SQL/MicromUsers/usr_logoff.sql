create or alter proc usr_logoff @username varchar(255) as

if @username is null
begin
	select	11, 'Username is null'
end

declare	@now datetime=getdate(), @login sysname = ORIGINAL_LOGIN()

begin try
	begin tran

		delete	a
		from	microm_users_devices a
				join microm_users b
				on(b.c_user_id=a.c_user_id)
		where	b.vc_username=@username

	commit tran
	select	0, 'OK'

end try
begin catch
	if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;
end catch

