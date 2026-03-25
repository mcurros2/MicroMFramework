create or alter proc eas_idrop
        @assembly_id Char(20)
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 51000, 'eas:idrop must be called within a transaction', 1

begin try
    
    delete  [entities_assemblies_types]
    where   c_assembly_id = @assembly_id

    delete  [entities_assemblies]
    where   c_assembly_id = @assembly_id

    select  @result=0, @msg='OK'

end try
begin catch

    throw;

end catch