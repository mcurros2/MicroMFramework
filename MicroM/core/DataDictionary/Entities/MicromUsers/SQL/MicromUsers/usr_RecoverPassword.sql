create or alter proc usr_RecoverPassword @username varchar(2048), @recovery_code varchar(255), @pwhash varchar(2048) as

declare	@now datetime=getdate(), @login sysname = ORIGINAL_LOGIN()

declare	@expired bit, @bad_logon_attempts int, @locked datetime, @disabled bit,
		@last_refresh datetime, @user_id char(20), @current_code varchar(255)

select	@last_refresh = b.dt_last_recovery
		, @current_code = b.vc_recovery_code
		, @locked = b.dt_locked
		, @disabled = b.bt_disabled
		, @user_id = b.c_user_id
from	microm_users b
where	b.vc_username = @username

if @user_id is null
begin
	select 11, 'Unknown username'
	return
end

if @disabled = 1
begin
	select 11, 'Account disabled'
	return
end

if @current_code is null
begin
	select 11, 'Invalid recovery code'
	return
end

if @recovery_code <> @current_code
begin
	select 11, 'Invalid recovery code'
	return
end

-- las refresh expires in 7 days, check if expired using datediff
if @last_refresh is not null and datediff(day, @last_refresh, @now) > 7
begin
	select 11, 'Recovery code expired'
	return
end

begin try

	begin tran

	update	microm_users
	set		vc_recovery_code = null
			, dt_last_recovery=@now
	where	c_user_id=@user_id

	exec usr_setPassword @username, @pwhash

	commit tran

end try
begin catch
	if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;
end catch
