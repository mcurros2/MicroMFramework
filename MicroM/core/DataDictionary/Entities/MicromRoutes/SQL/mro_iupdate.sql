create or alter proc mro_iupdate
        @route_id Char(20)
        , @route_path VarChar(2048)
        , @lu DateTime
        , @webusr VarChar(255)
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 50001, 'mro_iupdate must be called within a transaction', 1

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    select  @cu=dt_lu
            , @route_id = c_route_id
    from    [microm_routes] with (rowlock, holdlock, updlock)
    where   c_route_id = @route_id
            or vc_route_path = @route_path

    if @cu is null
    begin
        declare @id bigint
        exec num_iGetNewNumber 'mro', @nextnumber = @id out
        select @route_id = right('0000000000'+rtrim(@id),10)

        insert  [microm_routes]
        values
            (
            @route_id
            , @route_path
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )

        select    @result = 15, @msg = rtrim(@route_id)
        return
    end

    if @cu<>@lu or @lu is null 
    begin
        select @result = 4, @msg = 'Record changed'
        return
    end

    update  [microm_routes]
    set     vc_route_path = @route_path
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_route_id = @route_id

    select @result = 0, @msg = 'OK'

end try
begin catch

    throw;

end catch
