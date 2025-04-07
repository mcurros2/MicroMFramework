create or alter proc mug_update
        @user_group_id Char(20)
        , @user_group_name VarChar(255)
        , @group_members VarChar(max)
        , @lu DateTime
        , @webusr VarChar(255)
        as

set @group_members = NULLIF(@group_members,'')

begin try

    declare @result int, @msg varchar(255)

    begin tran

    exec    mug_iupdate
            @user_group_id
            , @user_group_name
            , @group_members
            , @lu
            , @webusr
            , @result = @result OUT
            , @msg = @msg OUT

    if @result not in(0,15)
    begin
        rollback
        select  @result, @msg
        return
    end

    commit tran
    select  @result, @msg

end try
begin catch

    if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;

end catch