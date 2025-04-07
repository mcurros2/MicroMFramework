create or alter proc usr_idrop
        @user_id Char(20)
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 51000, 'usr_idrop must be called within a transaction', 1

begin try

    delete  [microm_users_groups_members]
    where   c_user_id = @user_id

    delete  [microm_users_devices]
    where   c_user_id = @user_id

    delete  [microm_users_cat]
    where   c_user_id = @user_id

    delete  [microm_users]
    where   c_user_id = @user_id

    select  @result=0, @msg='OK'

end try
begin catch

    throw;

end catch
