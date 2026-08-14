create or alter proc [dbo].fcc_brwFiles
        @fileguid VarChar(255)
        , @fileprocess_id Char(20)
        , @like VarChar(80)
        , @d Char(1)
        as

select  a.vc_fileguid
        , [c_fileprocess_id] = rtrim(a.c_fileprocess_id)
        , a.vc_filename
        , [vc_filefolder] = rtrim(a.vc_filefolder)
        , a.bi_filesize
        , a.vc_file_tag
        , [c_fileuploadstatus_id] = rtrim(b.c_statusvalue_id)
        , [c_filestoragetype_id] = rtrim(c.c_categoryvalue_id)
from    [dbo].[file_store] a
        join [dbo].file_store_status b
		on(b.c_file_id = a.c_file_id and b.c_status_id='FileUpload')
        left join [dbo].[file_store_cat] c
        on(c.c_file_id = a.c_file_id and c.c_category_id = 'FileStorageTypes')
where   a.c_fileprocess_id = @fileprocess_id
        and (@fileguid is null or @fileguid = '' or a.vc_fileguid = @fileguid)
        and b.c_statusvalue_id <> 'Deleted'
