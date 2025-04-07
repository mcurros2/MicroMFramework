CREATE or ALTER proc [dbo].[usr_iupdate]
        @user_id Char(20)
		, @username VarChar(255)
		, @email VarChar(255)
		, @pwhash VarChar(2048)
		, @sid VarChar(85)
		, @badlogonattempts Int
		, @disabled bit
		, @locked DateTime
		, @last_login DateTime
		, @last_refresh DateTime
        , @recovery_code VarChar(255)
        , @last_recovery DateTime
        , @usertype_id char(20)
        , @user_groups varchar(max)
		, @lu DateTime
		, @webusr VarChar(80)
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 50001, 'usr_iupdate must be called within a transaction', 1

select @user_groups = nullif(@user_groups, '')

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    create table [#TempGroups] (user_group_id char(20))

    IF @user_groups IS NOT NULL
    BEGIN
        insert  [#TempGroups]
        select  rtrim(a.user_group_id)
        from    openjson(@user_groups) WITH (user_group_id varchar(max) '$') a
    END

    select  @cu=dt_lu
    from    [microm_users] with (rowlock, holdlock, updlock)
    where   c_user_id = @user_id

    if @cu is null
    begin
        
        declare @id bigint
		exec num_iGetNewNumber 'usr', @nextnumber = @id out
		select @user_id = right('0000000000'+rtrim(@id),10)

        insert  [microm_users]
        values
            (
            @user_id
			, @username
			, @email
			, @pwhash
			, @sid
			, isnull(@badlogonattempts,0)
			, isnull(@disabled,0)
			, @locked
			, @last_login
			, @last_refresh
            , null -- password recovery code
            , null -- last recovery
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )
        
        insert  [microm_users_cat]
        values  
            (
            @user_id
            , 'UserTypes'
            , @usertype_id
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )

        insert  microm_users_groups_members
        select  a.user_group_id
                , @user_id
                , @now
                , @now
                , @webusr
                , @webusr
                , @login
                , @login
        from    [#TempGroups] a

        select	@result = 15, @msg = rtrim(@user_id)
        return
    end
    
    if @cu<>@lu or @lu is null 
    begin
        select @result = 4, @msg = 'Record changed'
        return
    end

    if @usertype_id='ADMIN' and @disabled=1
    begin
        select @result = 11, @msg = 'Cannot disable the ADMIN user'
        return
    end

    update  [microm_users]
    set     vc_email = @email
			, bt_disabled = @disabled
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_user_id = @user_id
    
    update  [microm_users_cat]
    set     c_categoryvalue_id = @usertype_id
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_user_id = @user_id
            and c_category_id = 'UserTypes'

    -- delete groups
    delete  microm_users_groups_members
    WHERE   c_user_id = @user_id
            and c_user_group_id not in(SELECT user_group_id FROM [#TempGroups])

    -- insert new groups
    insert  microm_users_groups_members
    select  a.user_group_id
            , @user_id
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
    from    [#TempGroups] a
    where   not exists (
				select  1
				from    microm_users_groups_members
				where   c_user_group_id = a.user_group_id
                        and c_user_id = @user_id
				)

    select @result = 0, @msg = 'OK'
    
end try
begin catch

    throw;

end catch
