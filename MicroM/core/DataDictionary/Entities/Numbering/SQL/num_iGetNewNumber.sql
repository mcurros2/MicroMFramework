create or alter proc num_iGetNewNumber(@object_id char(20), @nextnumber bigint out) as

if @@trancount = 0 throw 50001, 'num_iGetNewNumber must be called within a transaction', 1

set @nextnumber=null

begin try

    select  @nextnumber = bi_lastnumber+1
    from    numbering with (holdlock, updlock, rowlock)
    where   c_object_id=@object_id

    if @nextnumber is not null
    begin
        update  numbering
        set     bi_lastnumber=@nextnumber
        where   c_object_id=@object_id
    end

end try
begin catch

    throw;
end catch
