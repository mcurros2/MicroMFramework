create or alter proc eas_idropUnusedAssemblies
        @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 51000, 'eas_idropUnusedAssemblies must be called within a transaction', 1

begin try

    select  a.c_assembly_id
    into    #delete
    from    entities_assemblies a
            left join applications_assemblies b
            on(b.c_assembly_id=a.c_assembly_id)
    where   b.c_assembly_id is null

    
    delete  a
    from    [entities_assemblies_types] a
            join #delete b
            on(b.c_assembly_id=a.c_assembly_id)

    delete  [entities_assemblies]
    from    [entities_assemblies] a
            join #delete b
            on(b.c_assembly_id=a.c_assembly_id)


    select  @result=0, @msg='OK'

end try
begin catch

    throw;

end catch