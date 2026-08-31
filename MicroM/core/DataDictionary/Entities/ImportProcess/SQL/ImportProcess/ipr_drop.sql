create or alter proc [dbo].ipr_drop  
        @import_process_id Char(20)  
        , @webusr varchar(255)
        as  
  
declare @now datetime=getdate(), @login sysname=original_login()

begin try  
  
    begin tran

    update  a
    set	    a.c_statusvalue_id='Deleted'
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    from    [dbo].file_store_status a
            join [dbo].file_store b
            on(b.c_file_id=a.c_file_id)
            join [dbo].[import_process] c
            on(c.c_fileprocess_id=b.c_fileprocess_id)
    where   c.c_import_process_id = @import_process_id
            and a.c_status_id = 'FileUpload'
  
    delete  [dbo].[import_process_errors]  
    where   c_import_process_id = @import_process_id  
  
    delete  [dbo].[import_process_status]  
    where   c_import_process_id = @import_process_id  
  
    delete  [dbo].[import_process]  
    where   c_import_process_id = @import_process_id  
  
    commit tran  
    select  0, 'OK'  
  
end try  
begin catch  
  
    if @@TRANCOUNT > 0  
    begin  
        rollback  
    end;  
  
    throw;  
  
end catch  