create or alter proc [dbo].usr_confirmTotp
	@username varchar(255)
as

if @username is null or @username=''
begin
	select 11, 'Username is null or empty'
	return
end

declare @now datetime=getdate(), @login sysname = ORIGINAL_LOGIN()

begin try
	begin tran

	update [dbo].microm_users
	set		bt_totp_enabled = case when vc_totp_secret is not null and vc_totp_secret<>'' then 1 else 0 end
			, dt_totp_confirmed = case when vc_totp_secret is not null and vc_totp_secret<>'' then @now else null end
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