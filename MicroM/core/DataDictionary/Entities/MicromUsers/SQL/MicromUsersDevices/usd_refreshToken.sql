create or alter proc usd_refreshToken @user_id char(20), @device_id varchar(255), @refreshtoken varchar(255), @new_refresh_token varchar(255), @refresh_expiration_hours int, @max_refresh_count int as

if @user_id is null
begin
	select	11, 'User ID is null'
end

if @device_id is null
begin
	select	11, 'Device ID is null'
end

declare	@now datetime=getdate(), @login sysname = ORIGINAL_LOGIN(), @refresh_expiration datetime = null

begin try
	begin tran

		declare	@expired bit, @actual_refresh_token varchar(255), @locked datetime, @refresh_counter int, @disabled bit

		-- MMC: get the user data
		select	@expired = case when b.dt_refresh_expiration is not null and b.dt_refresh_expiration>@now then 0 else 1 end
				, @actual_refresh_token = b.vc_refreshtoken
				, @locked = a.dt_locked
				, @disabled = a.bt_disabled
				, @refresh_counter = b.i_refreshcount
				, @refresh_expiration = b.dt_refresh_expiration
		from	microm_users a with (holdlock, rowlock, updlock)
				join microm_users_devices b
				on(b.c_user_id=a.c_user_id and b.c_device_id=@device_id)
		where	a.c_user_id=@user_id

		if @expired is null
		begin
			commit tran
			select [Status] = 11, [Message] = 'User ID not found'
			return
		end

		-- MMC: if the account is still locked return
		if @locked is not null and @locked < @now
		begin
			commit tran
			select [Status] = 13, [Message] = 'Account locked'
			return
		end

		if @disabled = 1
		begin
			-- MMC: invalidate actual token as account is disabled
			update	microm_users_devices
			set		vc_refreshtoken = null
					, dt_refresh_expiration = null
					, i_refreshcount = 0
					, dt_lu = @now
					, vc_luuser = @login
			where	c_user_id=@user_id
					and c_device_id=@device_id

			commit tran
			select [Status] = 14, [Message] = 'User ID disabled'
			return
		end

		if @expired = 1
		begin
			-- MMC: invalidate expired
			update	microm_users_devices
			set		vc_refreshtoken = null
					, dt_refresh_expiration = null
					, i_refreshcount = 0
					, dt_lu = @now
					, vc_luuser = @login
			where	c_user_id=@user_id
					and c_device_id=@device_id

			commit tran
			select [Status] = 9, [Message] = 'Refresh token expired'
			return
		end

		if @refresh_counter >= @max_refresh_count
		begin
			-- MMC: invalidate actual token as max refresh reached
			update	microm_users_devices
			set		vc_refreshtoken = null
					, dt_refresh_expiration = null
					, i_refreshcount = 0
					, dt_lu = @now
					, vc_luuser = @login
			where	c_user_id=@user_id
					and c_device_id=@device_id

			commit tran
			select [Status] = 10, [Message] = 'Max refresh reached'
			return
		end

		-- MMC: as the refresh token is per device, we issue a new token if refreshed.
		-- refresh tokens for other devices should remain valid
		if @actual_refresh_token = @refreshtoken
		begin
			--select	@refresh_expiration = dateadd(hh,@refresh_expiration_hours,@now)
			update	microm_users_devices
			set		vc_refreshtoken = @new_refresh_token
					-- MMC: the refresh expiration should be honored even when obtaining a new refresh token.
					-- it should only be renewed at login
					--, dt_refresh_expiration = @refresh_expiration
					, i_refreshcount += 1
					, dt_lu = @now
					, vc_luuser = @login
			where	c_user_id=@user_id
					and c_device_id=@device_id

			update	microm_users
			set		dt_last_refresh = @now
					, dt_lu = @now
					, vc_luuser = @login
			where	c_user_id=@user_id


			commit tran
			select [Status] = 0, [RefreshToken] = @new_refresh_token, [RefreshExpiration] = @refresh_expiration
			return
		end
		else
		begin
			-- MMC: update invalid attempt
			update	microm_users_devices
			set		i_refreshcount += 1
					, dt_lu = @now
					, vc_luuser = @login
			where	c_user_id=@user_id
					and c_device_id=@device_id

			commit tran
			select [Status] = 8, [Message] = 'Invalid Refresh Token'
			return
		end

		commit tran

		select [Status] = 11, [Message] = 'Unknown response'

end try
begin catch
	if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;
end catch

