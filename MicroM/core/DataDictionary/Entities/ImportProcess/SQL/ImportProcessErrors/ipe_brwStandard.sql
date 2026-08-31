create or alter proc [dbo].ipe_brwStandard
        @import_process_id Char(20)
        , @import_process_error_id Char(20)
        , @like VarChar(80)
        , @d Char(1)
        as

select  [Row]=b.i_row_number
        , [Error]=b.vc_error
from    [dbo].[import_process] a
        join [dbo].import_process_errors b
		on(b.c_import_process_id = a.c_import_process_id)
where   a.c_import_process_id=@import_process_id
