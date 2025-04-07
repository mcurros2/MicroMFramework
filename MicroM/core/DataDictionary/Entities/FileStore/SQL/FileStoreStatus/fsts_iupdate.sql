create or alter proc fsts_iupdate
        @file_id Char(20)
        , @status_id Char(20)
        , @statusvalue_id Char(20)
        , @lu DateTime
        , @webusr VarChar(80)
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 50001, 'fsts_iupdate must be called within a transaction', 1

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()
    
    select  @cu=dt_lu
    from    [file_store_status] with (rowlock, holdlock, updlock)
    where   c_file_id = @file_id
            and c_status_id = @status_id

    if @cu is null
    begin
        
        insert  [file_store_status]
        values
            (
            @file_id
            , @status_id
            , @statusvalue_id
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
    
    -- MMC: no concurrency, this is designed to be called from the backend

    update  [file_store_status]
    set     c_statusvalue_id = @statusvalue_id
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_file_id = @file_id
            and c_status_id = @status_id
    

    select @result = 0, @msg = 'OK'
    
end try
begin catch

    throw;

end catch
