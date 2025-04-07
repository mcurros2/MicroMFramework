create or alter proc fst_getByGUID
        @fileguid VarChar(255)
        as

select  [c_file_id] = rtrim(a.c_file_id)
        , [c_fileprocess_id] = rtrim(a.c_fileprocess_id)
        , a.vc_filename
        , [vc_filefolder] = rtrim(a.vc_filefolder)
        , a.vc_fileguid
        , a.bi_filesize
        , c_fileuploadstatus_id=rtrim(b.c_statusvalue_id)
from    [file_store] a
        join file_store_status b
		on(b.c_file_id = a.c_file_id and b.c_status_id='FileUpload')
where   a.vc_fileguid = @fileguid