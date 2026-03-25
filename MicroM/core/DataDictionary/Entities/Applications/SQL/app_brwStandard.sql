create or alter proc app_brwStandard
        @application_id Char(20)
		, @like VarChar(80)
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


select  [Application Id] = rtrim(a.c_application_id)
		, [Application Name] = a.vc_appname
		, [SQL Server] = a.vc_server
		, [Database] = a.vc_database
from    [applications] a
where   
        not exists (
            select  1
            from    [#like] l
            where   not 
            (
				a.c_application_id like l.phrase
				or a.vc_appname like l.phrase
				or a.vc_server like l.phrase
				or a.vc_database like l.phrase
            )
        )
