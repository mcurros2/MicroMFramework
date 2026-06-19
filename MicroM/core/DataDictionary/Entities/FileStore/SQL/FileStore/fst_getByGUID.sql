create or alter proc [dbo].fst_getByGUID
        @fileguid VarChar(255)
        as

select  [c_file_id] = rtrim(a.c_file_id)
        , [c_fileprocess_id] = rtrim(a.c_fileprocess_id)
        , a.vc_filename
        , [vc_filefolder] = rtrim(a.vc_filefolder)
        , a.vc_fileguid
        , a.bi_filesize
        , a.vc_file_tag
        , c_fileuploadstatus_id=rtrim(b.c_statusvalue_id)
        , c_filestoragetype_id=rtrim(c.c_categoryvalue_id)
from    [dbo].[file_store] a
        join [dbo].file_store_status b
		on(b.c_file_id = a.c_file_id and b.c_status_id='FileUpload')
        left join [dbo].file_store_cat c
        on(c.c_file_id = a.c_file_id and c.c_category_id='FileStorageTypes')
where   a.vc_fileguid = @fileguid