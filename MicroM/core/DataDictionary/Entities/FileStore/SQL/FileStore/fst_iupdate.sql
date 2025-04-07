create or alter proc fst_iupdate
        @file_id Char(20)
        , @fileprocess_id Char(20)
        , @filename VarChar(255)
        , @filefolder Char(6)
        , @fileguid VarChar(255)
        , @filesize bigint
        , @fileuploadstatus_id Char(20)
        , @lu DateTime
        , @webusr VarChar(80)
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 50001, 'fst_iupdate must be called within a transaction', 1

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    select  @fileprocess_id=nullif(@fileprocess_id,'')
    
    select  @cu=dt_lu
    from    [file_store] with (rowlock, holdlock, updlock)
    where   c_file_id = @file_id

    if @cu is null
    begin

        -- MMC: create a new file process if needed
        if @fileprocess_id is null
        begin
            exec fsp_iupdate null, @lu = null
                , @webusr = @webusr
                , @result = @result OUT
                , @msg = @msg OUT

            if @result not in(0,15)
            begin
                return
            end

            select @fileprocess_id = @msg
        end

        declare @id bigint
        exec num_iGetNewNumber 'fst', @nextnumber = @id out
        select @file_id = right('0000000000'+rtrim(@id),10)

        insert  [file_store]
        values
            (
            @file_id
            , @fileprocess_id
            , @filename
            , @filefolder
            , @fileguid
            , @filesize
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )
        
        insert  [file_store_status]
        select  @file_id
                , a.c_status_id
                , a.c_statusvalue_id
                , @now
                , @now
                , @webusr
                , @webusr
                , @login
                , @login
        from    status_values a
                join objects_status b
                on(b.c_status_id = a.c_status_id)
                join [objects] c
                on(c.c_object_id = b.c_object_id)
        where   c.c_mneo_id = 'fst' and
                a.bt_initial_value = 1

       
        select    @result = 15, @msg = rtrim(@file_id)
        return
    end

    if @cu<>@lu or @lu is null
    begin
        select @result = 4, @msg = 'Record changed'
        return
    end

    update  [file_store]
    set     bi_filesize = @filesize
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_file_id = @file_id
    

    select @result = 0, @msg = 'OK'
    
end try
begin catch

    throw;

end catch
