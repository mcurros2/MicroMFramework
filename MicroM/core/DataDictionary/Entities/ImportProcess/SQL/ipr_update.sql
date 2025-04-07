create or alter proc ipr_update
        @import_process_id Char(20)
        , @fileprocess_id Char(20)
        , @assemblytypename VarChar(2048)
        , @import_procname VarChar(2048)
        , @import_status_id Char(20)
        , @fileguid VarChar(255)
        , @lu DateTime
        , @webusr VarChar(255)
        as

if (@fileprocess_id is null or trim(@fileprocess_id) = '') begin select 11, 'The parameter @fileprocess_id cannot be null or empty' return end
if (@assemblytypename is null or trim(@assemblytypename) = '') begin select 11, 'The parameter @assemblytypename cannot be null or empty' return end

set @import_procname = NULLIF(@import_procname,'')
set @fileguid = NULLIF(@fileguid,'')

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    if exists(
		select	1 
		from	[file_store] 
		where	c_fileprocess_id=@fileprocess_id
				and vc_fileguid not like '%.csv'
		)
	begin
		select	11, 'You can only import .CSV files'
		return
	end

    if not exists(
		select	1 
		from	[file_store] 
		where	c_fileprocess_id=@fileprocess_id
				and vc_fileguid like '%.csv'
		)
	begin
		select	11, 'You need to upload a .CSV file'
		return
	end


    begin tran

    select  @cu=dt_lu
    from    [import_process] with (rowlock, holdlock, updlock)
    where   c_import_process_id = @import_process_id

    if @cu is null
    begin
        declare @id bigint
        exec num_iGetNewNumber 'ipr', @nextnumber = @id out
        select @import_process_id = right('0000000000'+rtrim(@id),10)

        insert  [import_process]
        values
            (
            @import_process_id
            , @fileprocess_id
            , @assemblytypename
            , @import_procname
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )

        insert  [import_process_status]
        select  @import_process_id
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
        where   c.c_mneo_id = 'ipr' and
                a.bt_initial_value = 1

        commit tran
        select    15, rtrim(@import_process_id)
        return
    end

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
