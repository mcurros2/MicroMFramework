create or alter proc [dbo].ipr_brwStandard
        @import_process_id Char(20)
        , @assemblytypename VarChar(2048)
        , @like VarChar(80)
        , @d Char(1)
        as

-- MMC: this is a special view used by importing by the data grid
select  [Process ID] = rtrim(a.c_import_process_id)
        , [File name]=d.vc_filename
        , [Processed]=a.i_total_records
        , [Errors]=a.i_errors
        , [Status]=c.vc_description
        , [Imported at]=a.dt_inserttime
        , [Imported by]=a.vc_webinsuser
from    [dbo].[import_process] a
        join [dbo].import_process_status b
		on(b.c_import_process_id = a.c_import_process_id and b.c_status_id='ImportStatus')
        join [dbo].status_values c
        on(c.c_status_id=b.c_status_id and c.c_statusvalue_id=b.c_statusvalue_id)
        join file_store d
        on(d.c_fileprocess_id=a.c_fileprocess_id)
where   a.vc_assemblytypename=@assemblytypename