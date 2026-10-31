create or alter proc [dbo].mme_InitializeMenu
    @json_menu varchar(max),
    @webusr varchar(255)
    as

set nocount on
declare @menu_id char(50) = json_value(@json_menu, '$.menu_id'),
        @menu_name varchar(255) = json_value(@json_menu, '$.menu_name'),
        @json_items varchar(max) = coalesce(json_query(@json_menu, '$.items'), '[]'),
        @error nvarchar(2048)

declare @status table (result int, msg varchar(max))

begin try
    begin tran

    insert @status 
    exec [dbo].mme_update
                @menu_id = @menu_id, @menu_name = @menu_name,
                @last_route_updated = null, @lu = null, @webusr = @webusr

    if exists (select 1 from @status where result not in (0, 4, 15))
    begin
        select top (1) @error = msg from @status where result not in (0, 4, 15);
        throw 51000, @error, 1;
    end

    exec [dbo].mmi_iCreateItems @menu_id = @menu_id, @json_items = @json_items, @webusr = @webusr
    exec [dbo].mir_iCreateItems @menu_id = @menu_id, @json_items = @json_items, @webusr = @webusr

    commit tran
    select 0, 'OK'
end try
begin catch
    if @@trancount > 0 rollback;
    throw;
end catch
