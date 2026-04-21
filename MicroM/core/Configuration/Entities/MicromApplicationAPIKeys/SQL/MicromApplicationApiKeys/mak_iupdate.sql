create or alter proc [dbo].mak_iupdate
        @application_id Char(20)
        , @api_key_id Char(20)
        , @apikey varchar(2048)
        , @secret VarChar(2048)
        , @change_secret bit
        , @lu DateTime
        , @webusr VarChar(255)
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 50001, 'mak_iupdate must be called within a transaction', 1

if (@change_secret = 1 and nullif(@apikey,'') is null) or (@lu is null and nullif(@apikey,'') is null)
begin
    select @result=11, @msg='The parameter @apikey cannot be null or empty' 
    return
end

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    select  @cu=dt_lu
    from    [dbo].[microm_application_api_keys] with (rowlock, holdlock, updlock)
    where   c_application_id = @application_id
            and c_api_key_id = @api_key_id

    if @cu is null
    begin
        declare @id bigint
        exec [dbo].num_iGetNewNumber 'mak', @nextnumber = @id out
        select @api_key_id = right('0000000000'+rtrim(@id),10)

        insert  [dbo].[microm_application_api_keys]
        values
            (
            @application_id
            , @api_key_id
            , @apikey
            , @secret
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )

        select    @result = 15, @msg = rtrim(@api_key_id)
        return
    end

    if @cu<>@lu or @lu is null 
    begin
        select @result = 4, @msg = 'Record changed'
        return
    end

    if @change_secret = 1
    begin
        update  [dbo].[microm_application_api_keys]
        set     vc_apikey = @apikey
                , vc_secret = @secret
                , vc_webluuser = @webusr
                , vc_luuser = @login
                , dt_lu = @now
        where   c_application_id = @application_id
                and c_api_key_id = @api_key_id
    end

    select @result = 0, @msg = 'OK'

end try
begin catch

    throw;

end catch
