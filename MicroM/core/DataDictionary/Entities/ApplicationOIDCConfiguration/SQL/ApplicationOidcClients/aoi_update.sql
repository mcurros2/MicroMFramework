create or alter proc aoi_update
        @application_id Char(20)
        , @client_app_id Char(20)
        , @api_key_id Char(20)
        , @url_authorized_redirects VarChar(max)
        , @url_sso_frontchannel_logout VarChar(2048)
        , @url_sso_backchannel_logout VarChar(2048)
        , @url_client_jwks VarChar(2048)
        , @certificate_unique_id VarChar(2048)
        , @oidc_subject_pepper VarChar(2048)
        , @apikey varchar(2048)
        , @secret VarChar(2048)
        , @change_secret bit
        , @lu DateTime
        , @webusr VarChar(255)
        as

if (@application_id is null or trim(@application_id) = '') begin select 11, 'The parameter @application_id cannot be null or empty' return end

if @change_secret = 1 and nullif(@apikey,'') is null
begin
    select 11, 'The parameter @apikey cannot be null or empty' 
    return
end

select  @api_key_id = null

set @url_authorized_redirects = NULLIF(@url_authorized_redirects,'')

create table [#TempRedirectUrls] (c_client_app_url_id char(20) null, vc_url_authorized_redirect varchar(2048) null)
IF @url_authorized_redirects IS NOT NULL
BEGIN
    insert  [#TempRedirectUrls]
    select  isnull(b.c_client_app_url_id, CONVERT(VARCHAR(20), CONVERT(BIGINT, CHECKSUM(a.vc_url_authorized_redirect)) & 0xFFFFFFFF))
            , trim(a.vc_url_authorized_redirect)
    from    openjson(@url_authorized_redirects) WITH (vc_url_authorized_redirect varchar(2048) '$') a
            left join application_oidc_clients_authorized_urls b
            on(b.c_application_id=@application_id and b.c_client_app_id=@client_app_id
            and b.vc_authorized_url=a.vc_url_authorized_redirect)
END

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()
            , @result int, @msg varchar(255), @apikey_lu datetime

    --select @apikey = convert(varchar(40),crypt_gen_random(20),2)

    begin tran

    select  @cu=a.dt_lu
            , @api_key_id = b.c_api_key_id
            , @apikey_lu = b.dt_lu
    from    [application_oidc_clients] a with (rowlock, holdlock, updlock)
            left join microm_application_api_keys b
            on(b.c_application_id=a.c_application_id and b.c_api_key_id=a.c_api_key_id)
    where   a.c_application_id = @application_id
            and a.c_client_app_id = @client_app_id


    if @cu is null
    begin

        declare @id bigint
        exec num_iGetNewNumber 'aoi', @nextnumber = @id out
        select @client_app_id = right('0000000000'+rtrim(@id),10)

        exec mak_iupdate 
            @application_id=@application_id
            , @api_key_id=null
            , @apikey=@apikey
            , @secret=@secret
            , @change_secret=@change_secret
            , @lu=null
            , @webusr=@webusr
            , @result=@result out
            , @msg=@msg out

        if @result not in(0,15)
        begin
            rollback
            select  @result, @msg
            return
        end

        select @api_key_id = @msg

        insert  [application_oidc_clients]
        values
            (
            @application_id
            , @client_app_id
            , @api_key_id
            , @url_sso_frontchannel_logout
            , @url_sso_backchannel_logout
            , @url_client_jwks
            , @certificate_unique_id
            , @oidc_subject_pepper
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )

        if @url_authorized_redirects is not null
        begin
            insert  application_oidc_clients_authorized_urls
            select  @application_id
                    , @client_app_id
                    , c_client_app_url_id
                    , vc_url_authorized_redirect
				    , @now
				    , @now
				    , @webusr
				    , @webusr
				    , @login
				    , @login
            from    #TempRedirectUrls
        end

        commit tran
        select    15, rtrim(@client_app_id)
        return
    end

    if @cu<>@lu or @lu is null 
    begin
        commit tran
        select 4, 'Record changed'
        return
    end

    update  [application_oidc_clients]
    set     vc_url_sso_backchannel_logout = @url_sso_backchannel_logout
            , vc_url_sso_frontchannel_logout = @url_sso_frontchannel_logout
            , vc_url_client_jwks = @url_client_jwks
            , vc_certificate_unique_id = @certificate_unique_id
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_application_id = @application_id
            and c_client_app_id = @client_app_id

    if @change_secret = 1
    begin
        exec mak_iupdate 
              @application_id=@application_id
            , @api_key_id=@api_key_id
            , @apikey=@apikey
            , @secret=@secret
            , @change_secret=@change_secret
            , @lu=@apikey_lu
            , @webusr=@webusr
            , @result=@result out
            , @msg=@msg out

        if @result not in(0,15)
        begin
            rollback
            select  @result, @msg
            return
        end
    end

    delete  application_oidc_clients_authorized_urls
    where   c_application_id = @application_id
            and c_client_app_id = @client_app_id
            and c_client_app_url_id not in (select c_client_app_url_id from [#TempRedirectUrls])

    insert  application_oidc_clients_authorized_urls
    select  @application_id
            , @client_app_id
            , c_client_app_url_id
            , a.vc_url_authorized_redirect
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
    from    [#TempRedirectUrls] a
    where   c_client_app_url_id not in 
            (   
                select  x.c_client_app_url_id 
                from    application_oidc_clients_authorized_urls x
                where   x.c_application_id=@application_id 
                        and x.c_client_app_id=@client_app_id
            )

    commit tran
    select 0, 'OK'

end try
begin catch

    if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;

end catch