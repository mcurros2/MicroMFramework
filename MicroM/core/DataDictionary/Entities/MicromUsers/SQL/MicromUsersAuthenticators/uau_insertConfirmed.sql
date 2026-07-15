create or alter proc [dbo].uau_insertConfirmed
    @user_id char(20)
    , @authenticator_name varchar(255)
    , @totp_secret varchar(2048)
as

if @user_id is null or @user_id=''
begin
    select 11, 'User ID is null or empty'
    return
end

if @authenticator_name is null or @authenticator_name=''
begin
    select 11, 'Authenticator name is null or empty'
    return
end

if @totp_secret is null or @totp_secret=''
begin
    select 11, 'TOTP secret is null or empty'
    return
end

declare @now datetime=getdate(), @login sysname=original_login(), @authenticator_id char(20), @id bigint

begin try
    begin tran

    if not exists(select 1 from [dbo].microm_users where c_user_id=@user_id)
    begin
        rollback
        select 11, 'User not found'
        return
    end

    exec [dbo].num_iGetNewNumber 'uau', @nextnumber = @id out
    select @authenticator_id = right('00000000000000000000'+rtrim(@id),20)

    insert [dbo].microm_users_authenticators
    values
        (
        @user_id
        , @authenticator_id
        , @authenticator_name
        , @totp_secret
        , @now
        , @now
        , ''
        , ''
        , @login
        , @login
        )

    commit tran
    select 0, rtrim(@authenticator_id)

end try
begin catch
    if @@trancount > 0
    begin
        rollback
    end;
    throw;
end catch
