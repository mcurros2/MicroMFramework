create or alter proc app_GetConfiguration  as

select	ApplicationID = rtrim(a.c_application_id)
		, ApplicationName = a.vc_appname
		, [SQLServer]  = a.vc_server
		, [SQLUser] = a.vc_user
		, [SQLPassword] = a.vc_password
		, SQLDB = a.vc_database
		, JWTIssuer = a.vc_JWTIssuer
		, JWTAudience = a.vc_JWTAudience
		, JWTKey = a.vc_JWTKey
		, JWTTokenExpirationMinutes = a.i_JWTTokenExpirationMinutes
		, JWTRefreshExpirationHours = a.i_JWTRefreshExpirationHours
		, AccountLockoutMinutes = a.i_AccountLockoutMinutes
		, MaxBadLogonAttempts = a.i_MaxBadLogonAttempts
		, MaxRefreshTokenAttempts = a.i_MaxRefreshTokenAttempts
		, AuthenticationType = rtrim(b.c_categoryvalue_id)
		, IdentityProviderRoleType = rtrim(c.c_categoryvalue_id)
		, IdentityProviderClients = d.clients
		, FrontendURLS=e.frontend_urls
from	applications a
		left join applications_cat b
		on(b.c_application_id=a.c_application_id and b.c_category_id='AuthenticationTypes')
		left join applications_cat c
		on(c.c_application_id=a.c_application_id and c.c_category_id='IdentityProviderRole')
		outer apply
		(
			select	clients='[' + string_agg('"'+replace(rtrim(x.c_client_app_id),'"', '\"')+'"', ',') + ']'
			from	application_oidc_clients x
			where	x.c_application_id=a.c_application_id
		) d
		outer apply
		(
			select	frontend_urls='[' + string_agg('"'+replace(rtrim(x.vc_application_url),'"', '\"')+'"', ',') + ']'
			from	applications_urls x
			where	x.c_application_id=a.c_application_id
		) e
