create or alter proc fst_idrop
        @file_id Char(20)
        , @fileguid varchar(255)
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 51000, 'fst_idrop must be called within a transaction', 1

select  @file_id=nullif(@file_id,'')
select  @fileguid=nullif(@fileguid,'')

if(@file_id is null and @fileguid is not null)
begin
	select  @file_id=c_file_id
	from    [file_store]
	where   vc_fileguid=@fileguid
end

begin try
    
    delete  file_store_status
    where   c_file_id = @file_id

    delete  [file_store]
    where   c_file_id = @file_id

    select  @result=0, @msg='OK'

end try
begin catch

    throw;

end catch

