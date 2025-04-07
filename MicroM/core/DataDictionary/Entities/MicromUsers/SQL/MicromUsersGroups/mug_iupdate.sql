create or alter proc mug_iupdate
        @user_group_id Char(20)
        , @user_group_name VarChar(255)
        , @group_members VarChar(max)
        , @lu DateTime
        , @webusr VarChar(255)
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 50001, 'mug_iupdate must be called within a transaction', 1

set @user_group_id = NULLIF(@user_group_id,'')

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    create table [#TempMembers] ([user_id] char(20))

    IF @group_members IS NOT NULL
    BEGIN
        insert  [#TempMembers]
        select  rtrim(a.[user_id])
        from    openjson(@group_members) WITH ([user_id] varchar(max) '$') a
    END

    select  @cu=dt_lu
    from    [microm_users_groups] with (rowlock, holdlock, updlock)
    where   c_user_group_id = @user_group_id

    if @cu is null
    begin
        declare @id bigint
        exec num_iGetNewNumber 'mug', @nextnumber = @id out
        select @user_group_id = case when @user_group_id is null then right('0000000000'+rtrim(@id),10) else @user_group_id end

        insert  [microm_users_groups]
        values
            (
            @user_group_id
            , @user_group_name
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )

        insert  microm_users_groups_members
        select  @user_group_id
                , a.[user_id]
                , @now
                , @now
                , @webusr
                , @webusr
                , @login
                , @login
        from    [#TempMembers] a

        select    @result=15, @msg=rtrim(@user_group_id)
        return
    end

    if @cu<>@lu or @lu is null 
    begin
        select @result=4, @msg='Record changed'
        return
    end

    update  [microm_users_groups]
    set     vc_user_group_name = @user_group_name
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_user_group_id = @user_group_id

    -- delete members
    delete  microm_users_groups_members
	where   c_user_group_id = @user_group_id
			and c_user_id not in (select [user_id] from [#TempMembers])

    -- insert new members
    insert  microm_users_groups_members
    select  @user_group_id
			, a.[user_id]
			, @now
			, @now
			, @webusr
			, @webusr
			, @login
			, @login
    from    [#TempMembers] a
	where   not exists 
            (
                select  1
                from    microm_users_groups_members b 
                where   b.c_user_group_id = @user_group_id 
                        and b.c_user_id = a.[user_id]
            )

    select @result=0, @msg='OK'

end try
begin catch

    throw;

end catch