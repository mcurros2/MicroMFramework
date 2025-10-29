create or alter proc aoc_iupdate
        @application_id Char(20)
        , @certificate_id Char(20)
        , @url_wellknown VarChar(2048)
        , @oidc_idp_subject_pepper VarChar(2048)
        , @lu DateTime
        , @webusr VarChar(255)
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 50001, 'aoc_iupdate must be called within a transaction', 1

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    select  @cu=dt_lu
    from    [application_oidc_configuration] with (rowlock, holdlock, updlock)
    where   c_application_id = @application_id

    if @cu is null
    begin

        insert  [application_oidc_configuration]
        values
            (
            @application_id
            , @certificate_id
            , @url_wellknown
            , @oidc_idp_subject_pepper
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

    update  [application_oidc_configuration]
    set     vc_url_wellknown = @url_wellknown
            , c_certificate_id = @certificate_id
            , vc_oidc_idp_subject_pepper = @oidc_idp_subject_pepper
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_application_id = @application_id

    select @result = 0, @msg = 'OK'

end try
begin catch

    throw;

end catch
