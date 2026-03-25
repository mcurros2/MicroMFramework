create or alter proc apa_GetAssemblies  as

select	ApplicationID = rtrim(a.c_application_id)
		, [AssemblyPath] = b.vc_assemblypath
		, [AssemblyID] = rtrim(a.c_assembly_id)
from		applications_assemblies a
		join entities_assemblies b
		on(b.c_assembly_id=a.c_assembly_id)

