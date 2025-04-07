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
from	Applications a

