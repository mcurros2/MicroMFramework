create or alter proc fsp_idrop
        @fileprocess_id Char(20)
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 51000, 'fsp_idrop must be called within a transaction', 1

begin try

    delete  a
    from    [file_store_status] a
            join [file_store] b
			on(a.c_file_id = b.c_file_id)
    where   b.c_fileprocess_id = @fileprocess_id

    delete  [file_store]
    where   c_fileprocess_id = @fileprocess_id
    
    delete  [file_store_process]
    where   c_fileprocess_id = @fileprocess_id

    select  @result=0, @msg='OK'

end try
begin catch

    throw;

end catch
