create or alter proc [dbo].[fsc_GetFileContent] @file_id char(20) as

if exists (select 1 from [dbo].file_store_status where c_file_id = @file_id and c_status_id = 'FileUpload' and c_statusvalue_id = 'Uploaded')
begin
	select	a.vb_file_content
	from	[dbo].file_store_content a
	where	a.c_file_id = @file_id
end
