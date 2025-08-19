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
		, @authenticationtype_id Char(20)
		, @assembly1 VarChar(2048)
		, @assembly2 VarChar(2048)
		, @assembly3 VarChar(2048)
		, @assembly4 VarChar(2048)
		, @assembly5 VarChar(2048)
		, @identity_provider_role_id char(20)
		, @oidc_url_wellknown VarChar(2048)
		, @oidc_url_jwks VarChar(2048)
		, @oidc_url_authorize VarChar(2048)
		, @oidc_url_token_backchannel VarChar(2048)
		, @oidc_url_endsession VarChar(2048)
		, @lu DateTime
		, @webusr VarChar(80)
        , @result int output
        , @msg varchar(255) output
        as


if @@trancount = 0 throw 50001, 'app_iupdate must be called within a transaction', 1

set @appurls = NULLIF(@appurls,'')

set @apiurl = isnull(@apiurl, '')

create table [#TempAppUrls] (c_application_url_id char(20) null, vc_application_url varchar(max))

IF @appurls IS NOT NULL
BEGIN
    insert  [#TempAppUrls]
    select  isnull(b.c_application_url_id, CONVERT(VARCHAR(20), CONVERT(BIGINT, CHECKSUM(a.vc_application_url)) & 0xFFFFFFFF))
            , trim(a.vc_application_url)
    from    openjson(@appurls) WITH (vc_application_url varchar(max) '$') a
            left join applications_urls b
            on(b.c_application_id=@application_id and b.c_application_url_id=a.vc_application_url)
END

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login(), @iresult int, @imsg varchar(255)

    select  @cu=dt_lu
    from    [applications] with (rowlock, holdlock, updlock)
    where   c_application_id = @application_id

    if @cu is null
    begin
        
        insert  [applications]
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
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )
        
        insert  [applications_cat]
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

        insert  [applications_cat]
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
            insert  [applications_urls]
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

        exec aois_iupdate @application_id, @oidc_url_wellknown, @oidc_url_jwks, @oidc_url_authorize, @oidc_url_token_backchannel, @oidc_url_endsession, null, @webusr, @result out, @msg out
        if @result not in(0, 15)
        begin
            return
        end

        exec apa_iupdateAssembly @application_id, null, @assembly1, 1, null, @webusr, @result out, @msg out
        if @result not in(0, 15)
        begin
            return
        end

        exec apa_iupdateAssembly @application_id, null, @assembly2, 2, null, @webusr, @result out, @msg out
        if @result not in(0, 15)
        begin
            return
        end

        exec apa_iupdateAssembly @application_id, null, @assembly3, 3, null, @webusr, @result out, @msg out
        if @result not in(0, 15)
        begin
            return
        end

        exec apa_iupdateAssembly @application_id, null, @assembly4, 4, null, @webusr, @result out, @msg out
        if @result not in(0, 15)
        begin
            return
        end

        exec apa_iupdateAssembly @application_id, null, @assembly5, 5, null, @webusr, @result out, @msg out
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

    update  [applications]
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
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_application_id = @application_id
    
    update  [applications_cat]
    set     c_categoryvalue_id = @authenticationtype_id
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_application_id = @application_id
			and c_category_id = 'AuthenticationTypes'


    if not exists
        (
            select  1
            from    [applications_cat] x
            where   x.c_application_id = @application_id
                    and x.c_category_id = 'IdentityProviderRole'
        )
    begin
        insert  [applications_cat]
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
        update  [applications_cat]
        set     c_categoryvalue_id = @identity_provider_role_id
                , vc_webluuser = @webusr
                , vc_luuser = @login
                , dt_lu = @now
        where   c_application_id = @application_id
			    and c_category_id = 'IdentityProviderRole'
    end

    delete  [applications_urls]
    WHERE   c_application_id = @application_id
            and c_application_url_id not in (select c_application_url_id from [#TempAppUrls])
    
    insert  [applications_urls]
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

    exec aois_iupdate @application_id, @oidc_url_wellknown, @oidc_url_jwks, @oidc_url_authorize, @oidc_url_token_backchannel, @oidc_url_endsession, null, @webusr, @result out, @msg out
    if @result not in(0, 15)
    begin
        return
    end

    exec apa_iupdateAssembly @application_id, null, @assembly1, 1, null, @webusr, @result out, @msg out
    if @result not in(0, 15)
    begin
        return
    end

    exec apa_iupdateAssembly @application_id, null, @assembly2, 2, null, @webusr, @result out, @msg out
    if @result not in(0, 15)
    begin
        return
    end

    exec apa_iupdateAssembly @application_id, null, @assembly3, 3, null, @webusr, @result out, @msg out
    if @result not in(0, 15)
    begin
        return
    end

    exec apa_iupdateAssembly @application_id, null, @assembly4, 4, null, @webusr, @result out, @msg out
    if @result not in(0, 15)
    begin
        return
    end

    exec apa_iupdateAssembly @application_id, null, @assembly5, 5, null, @webusr, @result out, @msg out
    if @result not in(0, 15)
    begin
        return
    end

    select @result = 0, @msg = 'OK'
    
end try
begin catch

    throw;

end catch
