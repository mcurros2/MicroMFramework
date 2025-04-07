create or alter proc mug_drop
        @user_group_id Char(20)
        as

begin try

    begin tran

    delete  [microm_users_groups_members]
    where   c_user_group_id = @user_group_id

    delete  [microm_users_groups_menus]
    where   c_user_group_id = @user_group_id

    delete  [microm_users_groups]
    where   c_user_group_id = @user_group_id

    commit tran
    select  0, 'OK'

end try
begin catch

    if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;

end catch