create or alter proc apt_brwStandard
        @application_id Char(20)
        , @assembly_id Char(20)
        , @order int
		, @assemblytype_id Char(20)
		, @like VarChar(max)
		, @d Char(1)
        as

set @like = NULLIF(@like,'')

create table [#like] (phrase varchar(max))

IF @like IS NOT NULL
BEGIN
    insert  [#like]
    select  phrase
    from    openjson(@like) WITH (phrase varchar(max) '$') a
END

select  [Assembly Id] = rtrim(b.c_assembly_id)
		, [Type Id] = rtrim(b.c_assemblytype_id)
		, [Type Name] = b.vc_assemblytypename
from    [applications_assemblies] a
		join entities_assemblies_types b
		on(b.c_assembly_id=a.c_assembly_id)
where   a.c_application_id = @application_id
		and
        not exists (
            select  1
            from    [#like] l
            where   not 
            (
                b.c_assemblytype_id like l.phrase
		        or b.c_assembly_id like l.phrase
		        or b.vc_assemblytypename like l.phrase
            )
        )