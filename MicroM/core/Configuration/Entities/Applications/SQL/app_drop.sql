create or alter proc [dbo].[app_drop]
        @application_id Char(20)
        as

begin try

    begin tran
    
    delete  [dbo].application_oidc_active_sessions
    where   c_application_id = @application_id

    delete  [dbo].application_oidc_configuration
    where   c_application_id = @application_id

    delete  [dbo].application_oidc_clients_authorized_urls
    where   c_application_id = @application_id

    delete  [dbo].application_oidc_clients
    where   c_application_id = @application_id

    delete  [dbo].microm_application_certificates
    where   c_application_id = @application_id

    delete  [dbo].[applications_assemblies]
    where   c_application_id = @application_id

    delete  [dbo].[applications_urls]
    where   c_application_id = @application_id

    delete  [dbo].[applications_cat]
    where   c_application_id = @application_id
    
    delete  [dbo].[applications]
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