create or alter proc aois_iupdate
        @application_id Char(20)
        , @url_wellknown VarChar(2048)
        , @url_jwks VarChar(2048)
        , @url_authorize VarChar(2048)
        , @url_token_backchannel VarChar(2048)
        , @url_endsession VarChar(2048)
        , @lu DateTime
        , @webusr VarChar(255)
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 50001, 'aois_iupdate must be called within a transaction', 1

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    select  @cu=dt_lu
    from    [application_oidc_server] with (rowlock, holdlock, updlock)
    where   c_application_id = @application_id

    if @cu is null
    begin

        insert  [application_oidc_server]
        values
            (
            @application_id
            , @url_wellknown
            , @url_jwks
            , @url_authorize
            , @url_token_backchannel
            , @url_endsession
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )

        select    @result = 0, @msg = 'OK'
        return
    end

    update  [application_oidc_server]
    set     vc_url_wellknown = @url_wellknown
            , vc_url_jwks = @url_jwks
            , vc_url_authorize = @url_authorize
            , vc_url_token_backchannel = @url_token_backchannel
            , vc_url_endsession = @url_endsession
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_application_id = @application_id

    select @result = 0, @msg = 'OK'

end try
begin catch

    throw;

end catch
