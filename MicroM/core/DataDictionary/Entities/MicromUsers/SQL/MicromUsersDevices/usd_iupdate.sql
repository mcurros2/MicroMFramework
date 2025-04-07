create or alter proc usd_iupdate
        @user_id Char(20)
        , @device_id VarChar(255)
        , @useragent VarChar(4096)
        , @ipaddress VarChar(40)
        , @refreshtoken VarChar(255)
        , @refresh_expiration DateTime
        , @refreshcount Int
        , @lu DateTime
        , @webusr VarChar(80)
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 50001, 'usd_iupdate must be called within a transaction', 1

if @user_id is null
begin
	select	@result=11, @msg='User ID is null'
    return
end

if @device_id is null
begin
	select	@result=11, @msg='Device ID is null'
    return
end

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    select  @cu=dt_lu
    from    [microm_users_devices] with (rowlock, holdlock, updlock)
    where   c_user_id = @user_id
            and c_device_id = @device_id

    if @cu is null
    begin
        
        insert  [microm_users_devices]
        values
            (
            @user_id
            , @device_id
            , @useragent
            , @ipaddress
            , @refreshtoken
            , @refresh_expiration
            , @refreshcount
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )
        
        
        select    @result = 0, @msg = 'OK'
        return
    end
    
    -- MMC: explicitly avoid LU

    update  [microm_users_devices]
    set     vc_refreshtoken = @refreshtoken
            , dt_refresh_expiration = @refresh_expiration
            , i_refreshcount = @refreshcount
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_user_id = @user_id
            and c_device_id = @device_id


    select @result = 0, @msg = 'OK'
    
end try
begin catch

    throw;

end catch
