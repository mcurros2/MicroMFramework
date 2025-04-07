create or alter proc usr_setPassword 
	@username varchar(255)
	, @pwhash varchar(2048)
as

if @username is null
begin
	select	11, 'Username is null'
end

if @pwhash is null or @pwhash=''
begin
	select	11, 'Password is null or empty'
end

declare	@now datetime=getdate(), @login sysname = ORIGINAL_LOGIN()

begin try
	begin tran

	update	microm_users
	set		vc_pwhash = @pwhash
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

