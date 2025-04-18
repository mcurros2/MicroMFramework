﻿create or alter proc app_get
        @application_id Char(20)
        as

declare @appurls VarChar(max)

select  @appurls = '[' + STRING_AGG('"'+replace(RTRIM(c_application_url_id), '"','\"')+'"', ',') + ']'
from    applications_urls
where   c_application_id = @application_id

;with asm as
(
select	b.c_application_id,
		b.i_order,
		a.vc_assemblypath
from		entities_assemblies a
		join applications_assemblies b
		on(b.c_assembly_id=a.c_assembly_id)
)

select  [c_application_id] = rtrim(a.c_application_id)
		, a.vc_appname
		, vc_appurls=@appurls
		, a.vc_apiurl
		, a.vc_server
		, a.vc_user
		, a.vc_password
		, a.vc_database
		, a.vc_app_admin_user
		, a.vc_app_admin_password
		, a.vc_JWTIssuer
		, a.vc_JWTAudience
		, a.vc_JWTKey
		, a.i_JWTTokenExpirationMinutes
		, a.i_JWTRefreshExpirationHours
		, a.i_AccountLockoutMinutes
		, a.i_MaxBadLogonAttempts
		, a.i_MaxRefreshTokenAttempts
		, [c_categoryvalue_id] = rtrim(b.c_categoryvalue_id)
		, vc_assembly1 = (select x.vc_assemblypath from asm x where x.c_application_id=a.c_application_id and x.i_order=1)
		, vc_assembly2 = (select x.vc_assemblypath from asm x where x.c_application_id=a.c_application_id and x.i_order=2)
		, vc_assembly3 = (select x.vc_assemblypath from asm x where x.c_application_id=a.c_application_id and x.i_order=3)
		, vc_assembly4 = (select x.vc_assemblypath from asm x where x.c_application_id=a.c_application_id and x.i_order=4)
		, vc_assembly5 = (select x.vc_assemblypath from asm x where x.c_application_id=a.c_application_id and x.i_order=5)
		, b_createdatabase = convert(bit,0)
        , b_dropdatabase = convert(bit,0)
        , b_adminuserhasrights = convert(bit,0)
        , b_appdbexists = convert(bit,0)
        , b_appuserexists = convert(bit,0)
        , b_serverup = convert(bit,0)
		, a.dt_inserttime
		, a.dt_lu
		, a.vc_webinsuser
		, a.vc_webluuser
		, a.vc_insuser
		, a.vc_luuser
from    [applications] a
		join [applications_cat] b
		on(a.c_application_id = b.c_application_id
		and b.c_category_id = 'AuthenticationTypes')
where   a.c_application_id = @application_id
