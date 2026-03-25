create or alter proc [dbo].[eat_deleteAllTypes]
		@assembly_id char(20)
        as

begin try

    delete  entities_assemblies_types
    where   c_assembly_id=@assembly_id

    select 0, 'OK'

end try
begin catch

    throw;

end catch
