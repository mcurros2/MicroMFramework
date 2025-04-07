create or alter proc [dbo].[app_drop]
        @application_id Char(20)
        as

begin try

    begin tran
    
    delete  [applications_assemblies]
    where   c_application_id = @application_id

    delete  [applications_urls]
    where   c_application_id = @application_id

    delete  [applications_cat]
    where   c_application_id = @application_id
    
    delete  [applications]
    where   c_application_id = @application_id

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