create or alter proc [dbo].eat_GetAssemblyTypes
    @assembly_id char(20)
    as

select  vc_assemblytypename
from    [dbo].entities_assemblies_types
where   c_assembly_id = @assembly_id
