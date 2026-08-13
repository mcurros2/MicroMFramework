create or alter proc [dbo].fcc_drop
        @fileguid VarChar(255)
        as

set @fileguid = nullif(ltrim(rtrim(@fileguid)), '')

if @fileguid is null
begin
    select 11, 'File GUID is required'
    return
end

begin try
    begin tran

    declare @file_id Char(20), @now DateTime = getdate(), @login sysname = original_login()

    select  @file_id = c_file_id
    from    [dbo].[file_store] with (rowlock, holdlock, updlock)
    where   vc_fileguid = @fileguid

    if @file_id is null
    begin
        rollback tran
        select 11, 'File GUID was not found'
        return
    end

    update  [dbo].[file_store_status]
    set     c_statusvalue_id = 'Deleted'
            , vc_luuser = @login
            , dt_lu = @now
    where   c_file_id = @file_id
            and c_status_id = 'FileUpload'

    commit tran
    select 0, 'OK'
end try
begin catch
    if @@trancount > 0 rollback tran
    throw
end catch
