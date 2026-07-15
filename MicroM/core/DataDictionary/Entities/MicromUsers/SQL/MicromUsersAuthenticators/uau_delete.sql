create or alter proc [dbo].uau_delete
    @user_id char(20)
    , @authenticator_id char(20)
as

if @user_id is null or @user_id=''
begin
    select 11, 'User ID is null or empty'
    return
end

if @authenticator_id is null or @authenticator_id=''
begin
    select 11, 'Authenticator ID is null or empty'
    return
end

begin try
    begin tran

    delete [dbo].microm_users_authenticators
    where c_user_id=@user_id
      and c_authenticator_id=@authenticator_id

    commit tran
    select 0, 'OK'

end try
begin catch
    if @@trancount > 0
    begin
        rollback
    end;
    throw;
end catch
