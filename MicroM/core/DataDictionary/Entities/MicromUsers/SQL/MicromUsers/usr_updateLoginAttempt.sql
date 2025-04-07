create or alter proc usr_updateLoginAttempt 
		@user_id char(20), @new_refresh_token varchar(255), @success bit, @account_lockout_mins int, @refresh_expiration_hours int, @max_bad_logon_attempts int,
		@device_id varchar(255), @user_agent varchar(4096), @ipaddress varchar(40) as

if @user_id is null
begin
	select	11, 'User ID is null'
end

declare	@now datetime=getdate(), @login sysname = ORIGINAL_LOGIN()

begin try
	begin tran

		declare	@expired bit, @actual_refresh_token varchar(255), @bad_logon_attempts int, @locked datetime, @disabled bit,
				@errn int, @msg varchar(255), @new_refresh_expiration datetime

		-- MMC: get the user data
		select	@expired = case when b.dt_refresh_expiration is not null and b.dt_refresh_expiration>@now then 0 else 1 end
				, @actual_refresh_token = b.vc_refreshtoken
				, @bad_logon_attempts = a.i_badlogonattempts
				, @locked = a.dt_locked
				, @disabled = a.bt_disabled
		from	microm_users a with (holdlock, rowlock, updlock)
				left join microm_users_devices b with (holdlock, rowlock, updlock)
				on(b.c_user_id=a.c_user_id and b.c_device_id=@device_id)
		where	a.c_user_id=@user_id
				

		if @bad_logon_attempts is null
		begin
			commit tran
			select 11, 'User ID not found'
			return
		end

		-- MMC: if the account is still locked return
		if @locked is not null and @locked > @now
		begin
			commit tran
			select 13, 'Account locked'
			return
		end

		-- MMC: update history
		set  @errn = -1
		exec ulh_iupdate null, @user_id, @user_agent, @ipaddress, @success, @now, @now, @login, @errn out, @msg out

		-- MMC: password hash verification was successful
		if @success = 1
		begin
				-- MMC: we always unlock the account as the user has logged in providing his password
				-- and update the refresh token to the new one provided
				update	microm_users
				set		dt_locked = null
						, i_badlogonattempts = 0
						, dt_last_login = @now
						, dt_lu = @now
						, vc_luuser = @login
				where	c_user_id=@user_id

				-- MMC: we renew the refresh token if there is no one or the existing one has expired
				if @actual_refresh_token is null or @expired = 1
				begin
					select	@new_refresh_expiration = dateadd(hh,@refresh_expiration_hours,@now)
		
					set @errn = -1
					exec usd_iupdate @user_id, @device_id, @user_agent, @ipaddress, @new_refresh_token, @new_refresh_expiration, 0, null, '', @errn out, @msg out

					if @errn not in(0,15)
					begin
						if @@trancount > 0 rollback
						select 11, @msg
						return
					end
				end

				commit tran
				-- MMC: if the device exists and a refresh token exists we return the same refresh token.
				-- the token will be renewed when the refreshToekn endpoint is called. If not renewed before it expires, the user will need to login again.
				select	0, case when @actual_refresh_token is null or @expired=1 then @new_refresh_token else @actual_refresh_token end
		end
		else 
		begin

			if @bad_logon_attempts+1 >= @max_bad_logon_attempts
			begin
				-- MMC: max bad logon attemps has been reached so we lock the account and clear any refresh token, the user will need to logon with their valid password to unlock
				update	microm_users
				set		dt_locked = dateadd(mi,@account_lockout_mins,@now)
						, i_badlogonattempts += 1
						, dt_lu = @now
						, vc_luuser = @login
				where	c_user_id=@user_id

				-- MMC: the account was locked. we invalidate all refresh tokens and devices. This will cause the user to login again after the login token expires
				delete	microm_users_devices
				where	c_user_id=@user_id

			end
			else 
			begin
				

				-- MMC: for bad passwords attemps, we will not invalidate the existing refresh token
				-- just update the counter
				update	microm_users
				set		i_badlogonattempts += 1
						, dt_lu = @now
						, vc_luuser = @login
				where	c_user_id=@user_id

			end

			commit tran
			select	0, null

		end

end try
begin catch
	if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;
end catch

