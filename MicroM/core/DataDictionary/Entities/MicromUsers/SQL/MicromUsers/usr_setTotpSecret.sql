create or alter proc [dbo].usr_setTotpSecret
	@username varchar(255)
	, @totp_secret varchar(2048)
as

if @username is null or @username=''
begin
	select 11, 'Username is null or empty'
	return
end

if @totp_secret is null or @totp_secret=''
begin
	select 11, 'TOTP secret is null or empty'
	return
end

declare @now datetime=getdate(), @login sysname = ORIGINAL_LOGIN()

begin try
	begin tran

	update [dbo].microm_users
	set		vc_totp_secret = @totp_secret
			, dt_lu = @now
			, vc_luuser = @login
	where	vc_username=@username

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
