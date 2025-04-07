create or alter proc ipr_get
        @import_process_id Char(20)
        as

select  [c_import_process_id] = rtrim(a.c_import_process_id)
        , [c_fileprocess_id] = rtrim(a.c_fileprocess_id)
        , a.vc_assemblytypename
        , a.vc_import_procname
        , [c_import_status_id] = rtrim(b.c_statusvalue_id)
        , vc_fileguid=c.vc_fileguid /* fake column vc_fileguid */
        , a.dt_inserttime
        , a.dt_lu
        , a.vc_webinsuser
        , a.vc_webluuser
        , a.vc_insuser
        , a.vc_luuser
from    [import_process] a
        join [import_process_status] b
        on(a.c_import_process_id = b.c_import_process_id
        and b.c_status_id = 'ImportStatus')
        join file_store c
        on(a.c_fileprocess_id = c.c_fileprocess_id)
where   a.c_import_process_id = @import_process_id