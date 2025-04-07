create or alter proc ipr_UpdateStatus
		@import_process_id Char(20)
		, @import_status_id Char(20)
		, @webusr VarChar(255)
		as

if (@import_process_id is null or trim(@import_process_id) = '') begin select 11, 'The parameter @import_process_id cannot be null or empty' return end
if (@import_status_id is null or trim(@import_status_id) = '') begin select 11, 'The parameter @import_status_id cannot be null or empty' return end

begin try
	declare @now datetime=getdate(), @login sysname=original_login()

	update  [import_process_status]
	set     c_statusvalue_id = @import_status_id
			, dt_lu = @now
			, vc_webluuser = @webusr
			, vc_luuser = @login
	where   c_import_process_id = @import_process_id
			and c_status_id = 'ImportStatus'

	select 0, 'OK'
	return

end try
begin catch
	if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;
end catch