create or alter proc fst_brwFiles
        @file_id Char(20)
        , @fileprocess_id Char(20)
        , @like VarChar(80)
        , @d Char(1)
        as

-- MMC: this is a special view used by file uploader
select  [c_file_id] = rtrim(a.c_file_id)
        , a.vc_filename
        , [vc_filefolder] = rtrim(a.vc_filefolder)
        , a.vc_fileguid
        , c_fileuploadstatus_id=rtrim(b.c_statusvalue_id)
        , a.bi_filesize
from    [file_store] a
        join file_store_status b
		on(b.c_file_id = a.c_file_id and b.c_status_id='FileUpload')
where   a.c_fileprocess_id = @fileprocess_id
