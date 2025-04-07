create or alter proc usr_resetPassword 
	@username varchar(255)
as

if @username is null
begin
	select	11, 'Username is null'
end

declare	@now datetime=getdate(), @login sysname = ORIGINAL_LOGIN()

begin try
	begin tran

	-- MMC: reset the hash, this hash will never validate the signature
	update	microm_users
	set		vc_pwhash = convert(varchar,NEWID())
			, dt_locked = null
			, i_badlogonattempts = 0
			, dt_lu = @now
			, vc_luuser = @login
	where	vc_username=@username

	delete	a
	from	microm_users_devices a
			join microm_users b
			on(b.c_user_id=a.c_user_id)
	where	b.vc_username = @username

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

