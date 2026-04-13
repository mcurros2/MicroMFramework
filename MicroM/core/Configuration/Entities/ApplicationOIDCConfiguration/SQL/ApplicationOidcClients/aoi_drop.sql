create or alter proc [dbo].aoi_drop
        @application_id Char(20)
        , @client_app_id Char(20)
        as

begin try

    begin tran

    delete  [dbo].[application_oidc_clients_authorized_urls]
    where   c_application_id = @application_id
            and c_client_app_id = @client_app_id

    delete  [dbo].[application_oidc_clients]
    where   c_application_id = @application_id
            and c_client_app_id = @client_app_id

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