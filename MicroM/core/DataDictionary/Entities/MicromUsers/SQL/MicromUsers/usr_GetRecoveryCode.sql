create or alter proc usr_GetRecoveryCode @username varchar(255) as

declare	@now datetime=getdate(), @login sysname = ORIGINAL_LOGIN()

declare	@expired bit, @recovery_code varchar(255), @bad_logon_attempts int, @locked datetime, @disabled bit,
		@last_refresh datetime, @new_refresh_expiration datetime, @user_id char(20)

select	@last_refresh = b.dt_last_recovery
		, @recovery_code = b.vc_recovery_code
		, @locked = b.dt_locked
		, @disabled = b.bt_disabled
		, @user_id = b.c_user_id
from	microm_users b
where	b.vc_username=@username

if @user_id is null
begin
	select 11, 'User ID not found'
	return
end

if @disabled = 1
begin
	select 11, 'Account disabled'
	return
end

-- MMC: if the account is still locked return
if @locked is not null and @locked > @now
begin
	select 11, 'Account locked'
	return
end

begin try
	begin tran

	select	@recovery_code=NEWID()

	update	microm_users
	set		vc_recovery_code=@recovery_code
			, dt_last_recovery=@now
	where	c_user_id=@user_id

	commit tran

	select	15, @recovery_code

end try
begin catch
	if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;
end catch