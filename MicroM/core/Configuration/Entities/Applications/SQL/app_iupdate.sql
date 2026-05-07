create or alter proc [dbo].[app_iupdate]
        @application_id Char(20)
		, @appname VarChar(255)
		, @appurls VarChar(max)
		, @apiurl VarChar(2048)
		, @server VarChar(255)
		, @user VarChar(255)
		, @password VarChar(2048)
		, @database VarChar(255)
		, @app_admin_user VarChar(255)
		, @app_admin_password VarChar(2048)
		, @JWTIssuer VarChar(255)
		, @JWTAudience VarChar(255)
		, @JWTKey VarChar(255)
		, @JWTTokenExpirationMinutes Int
		, @JWTRefreshExpirationHours Int
		, @AccountLockoutMinutes Int
		, @MaxBadLogonAttempts Int
		, @MaxRefreshTokenAttempts Int
        , @app_schema VarChar(50)
        , @datadictionary_schema VarChar(50)
        , @enable_seed_test_data bit
        , @enable_developer_tools bit
        , @ts_categories_folder varchar(255)
        , @ts_categories_values_class_name varchar(255)
        , @ts_categories_values_class_import varchar(255)
		, @authenticationtype_id Char(20)
		, @assembly1 VarChar(2048)
		, @assembly2 VarChar(2048)
		, @assembly3 VarChar(2048)
		, @assembly4 VarChar(2048)
		, @assembly5 VarChar(2048)
		, @identity_provider_role_id char(20)
		, @oidc_url_wellknown VarChar(2048)
        , @oidc_idp_subject_pepper varchar(2048)
        , @certificate_unique_id varchar(2048)
        , @certificate_blob varbinary(max)
        , @certificate_password varchar(2048)
		, @lu DateTime
		, @webusr VarChar(80)
        , @result int output
        , @msg varchar(255) output
        as


if @@trancount = 0 throw 50001, 'app_iupdate must be called within a transaction', 1

set @appurls = NULLIF(@appurls,'')

set @apiurl = isnull(@apiurl, '')

set @enable_seed_test_data = isnull(@enable_seed_test_data, 0)
set @enable_developer_tools = isnull(@enable_developer_tools, 0)

create table [#TempAppUrls] (c_application_url_id char(20) null, vc_application_url varchar(max))

IF @appurls IS NOT NULL
BEGIN
    insert  [#TempAppUrls]
    select  isnull(b.c_application_url_id, CONVERT(VARCHAR(20), CONVERT(BIGINT, CHECKSUM(a.vc_application_url)) & 0xFFFFFFFF))
            , trim(a.vc_application_url)
    from    openjson(@appurls) WITH (vc_application_url varchar(max) '$') a
            left join [dbo].applications_urls b
            on(b.c_application_id=@application_id and b.c_application_url_id=a.vc_application_url)
END

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login(), @iresult int, @imsg varchar(255)
            , @certificate_id char(20), @certificate_lu datetime, @certificate_guid_id uniqueidentifier

    select  @certificate_guid_id = convert(uniqueidentifier, @certificate_unique_id)

    select  @cu=a.dt_lu
            , @certificate_id=b.c_certificate_id
            , @certificate_lu=b.dt_lu
    from    [dbo].[applications] a with (rowlock, holdlock, updlock)
            left join [dbo].microm_application_certificates b
            on(a.c_application_id = b.c_application_id and b.ui_certificate_guid_id=@certificate_guid_id)
    where   a.c_application_id = @application_id

    if @cu is null
    begin
        
        insert  [dbo].[applications]
        values
            (
            @application_id
			, @appname
			, @apiurl
			, @server
			, @user
			, @password
			, @database
            , @app_admin_user
            , @app_admin_password
			, @JWTIssuer
			, @JWTAudience
			, @JWTKey
			, @JWTTokenExpirationMinutes
			, @JWTRefreshExpirationHours
			, @AccountLockoutMinutes
			, @MaxBadLogonAttempts
			, @MaxRefreshTokenAttempts
            , @app_schema
            , @datadictionary_schema
            , @enable_seed_test_data
            , @enable_developer_tools
            , @ts_categories_folder
            , @ts_categories_values_class_name
            , @ts_categories_values_class_import
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )
        
        insert  [dbo].[applications_cat]
        values  
            (
            @application_id
			, 'AuthenticationTypes'
			, @authenticationtype_id
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )

        insert  [dbo].[applications_cat]
        values  
            (
            @application_id
			, 'IdentityProviderRole'
			, @identity_provider_role_id
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )

        if @appurls is not null
        begin
            insert  [dbo].[applications_urls]
            select  @application_id
				    , c_application_url_id
                    , vc_application_url
				    , @now
				    , @now
				    , @webusr
				    , @webusr
				    , @login
				    , @login
            from    [#TempAppUrls]
        end

        exec [dbo].mac_iupdate @application_id, null, @certificate_unique_id, @certificate_blob, @certificate_password, null, @webusr, @iresult out, @imsg out
        if @iresult not in(0, 15)
        begin
            select @result = @iresult, @msg = @imsg
            return
        end

        select @certificate_id = @imsg

        exec [dbo].aoc_iupdate @application_id, @certificate_id, @oidc_url_wellknown, @oidc_idp_subject_pepper, null, @webusr, @result out, @msg out
        if @result not in(0, 15)
        begin
            return
        end

        exec [dbo].apa_iupdateAssembly @application_id, null, @assembly1, 1, null, @webusr, @result out, @msg out
        if @result not in(0, 15)
        begin
            return
        end

        exec [dbo].apa_iupdateAssembly @application_id, null, @assembly2, 2, null, @webusr, @result out, @msg out
        if @result not in(0, 15)
        begin
            return
        end

        exec [dbo].apa_iupdateAssembly @application_id, null, @assembly3, 3, null, @webusr, @result out, @msg out
        if @result not in(0, 15)
        begin
            return
        end

        exec [dbo].apa_iupdateAssembly @application_id, null, @assembly4, 4, null, @webusr, @result out, @msg out
        if @result not in(0, 15)
        begin
            return
        end

        exec [dbo].apa_iupdateAssembly @application_id, null, @assembly5, 5, null, @webusr, @result out, @msg out
        if @result not in(0, 15)
        begin
            return
        end
       
        select	@result = 0, @msg = 'OK'
        return
    end
    
    if @cu<>@lu or @lu is null 
    begin
        select @result = 4, @msg = 'Record changed'
        return
    end

    update  [dbo].[applications]
    set     vc_appname = @appname
			, vc_apiurl = @apiurl
			, vc_server = @server
			, vc_user = @user
			, vc_app_admin_user = @app_admin_user
			, vc_password = @password
			, vc_database = @database
			, vc_JWTIssuer = @JWTIssuer
			, vc_JWTAudience = @JWTAudience
			, vc_JWTKey = @JWTKey
			, i_JWTTokenExpirationMinutes = @JWTTokenExpirationMinutes
			, i_JWTRefreshExpirationHours = @JWTRefreshExpirationHours
			, i_AccountLockoutMinutes = @AccountLockoutMinutes
			, i_MaxBadLogonAttempts = @MaxBadLogonAttempts
			, i_MaxRefreshTokenAttempts = @MaxRefreshTokenAttempts
            , vc_app_schema = @app_schema
            , vc_datadictionary_schema = @datadictionary_schema
            , b_enable_seed_test_data = @enable_seed_test_data
            , b_enable_developer_tools = @enable_developer_tools
            , vc_ts_categories_folder = @ts_categories_folder
            , vc_ts_dd_categories_values_class_name = @ts_categories_values_class_name
            , vc_ts_dd_categories_values_class_import = @ts_categories_values_class_import
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_application_id = @application_id
    
    update  [dbo].[applications_cat]
    set     c_categoryvalue_id = @authenticationtype_id
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_application_id = @application_id
			and c_category_id = 'AuthenticationTypes'


    if not exists
        (
            select  1
            from    [dbo].[applications_cat] x
            where   x.c_application_id = @application_id
                    and x.c_category_id = 'IdentityProviderRole'
        )
    begin
        insert  [dbo].[applications_cat]
        values  
            (
            @application_id
			, 'IdentityProviderRole'
			, @identity_provider_role_id
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )
    end
    else
    begin
        update  [dbo].[applications_cat]
        set     c_categoryvalue_id = @identity_provider_role_id
                , vc_webluuser = @webusr
                , vc_luuser = @login
                , dt_lu = @now
        where   c_application_id = @application_id
			    and c_category_id = 'IdentityProviderRole'
    end

    delete  [dbo].[applications_urls]
    WHERE   c_application_id = @application_id
            and c_application_url_id not in (select c_application_url_id from [#TempAppUrls])
    
    insert  [dbo].[applications_urls]
    select  @application_id
			, c_application_url_id
            , vc_application_url
			, @now
			, @now
			, @webusr
			, @webusr
			, @login
			, @login
    from    [#TempAppUrls] a
    where   c_application_url_id not in 
            (   
                select  x.c_application_url_id 
                from    [applications_urls] x
                where   x.c_application_id=@application_id 
            )

    if @certificate_id is null
    begin
        exec [dbo].mac_iupdate @application_id, null, @certificate_unique_id, @certificate_blob, @certificate_password, null, @webusr, @iresult out, @imsg out
        if @iresult not in(0, 15)
        begin
            select @result = @iresult, @msg = @imsg
            return
        end
        select @certificate_id = @imsg
    end

    exec [dbo].aoc_iupdate @application_id, @certificate_id, @oidc_url_wellknown, @oidc_idp_subject_pepper, null, @webusr, @result out, @msg out
    if @result not in(0, 15)
    begin
        return
    end

    exec [dbo].apa_iupdateAssembly @application_id, null, @assembly1, 1, null, @webusr, @result out, @msg out
    if @result not in(0, 15)
    begin
        return
    end

    exec [dbo].apa_iupdateAssembly @application_id, null, @assembly2, 2, null, @webusr, @result out, @msg out
    if @result not in(0, 15)
    begin
        return
    end

    exec [dbo].apa_iupdateAssembly @application_id, null, @assembly3, 3, null, @webusr, @result out, @msg out
    if @result not in(0, 15)
    begin
        return
    end

    exec [dbo].apa_iupdateAssembly @application_id, null, @assembly4, 4, null, @webusr, @result out, @msg out
    if @result not in(0, 15)
    begin
        return
    end

    exec [dbo].apa_iupdateAssembly @application_id, null, @assembly5, 5, null, @webusr, @result out, @msg out
    if @result not in(0, 15)
    begin
        return
    end

    select @result = 0, @msg = 'OK'
    
end try
begin catch

    throw;

end catch
