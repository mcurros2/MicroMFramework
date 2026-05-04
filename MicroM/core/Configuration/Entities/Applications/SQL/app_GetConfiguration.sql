create or alter proc [dbo].app_GetConfiguration  as

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
		, OIDCWellKnownURL=d.vc_url_wellknown
		, OIDCCertificateUniqueID=convert(varchar(2048),f.ui_certificate_guid_id)
		, OIDCCertificateBlob = f.vb_certificate_blob
		, OIDCCertificatePassword = f.vc_certificate_password
		, OIDCIdPSubjectPepper=d.vc_oidc_idp_subject_pepper
		, FrontendURLS=e.frontend_urls
		, APPSchema = a.vc_app_schema
		, DDSchema = a.vc_datadictionary_schema
		, EnableSeedTestData = a.b_enable_seed_test_data
		, EnableDeveloperTools = a.b_enable_developer_tools
from	[dbo].applications a
		left join [dbo].applications_cat b
		on(b.c_application_id=a.c_application_id and b.c_category_id='AuthenticationTypes')
		left join [dbo].applications_cat c
		on(c.c_application_id=a.c_application_id and c.c_category_id='IdentityProviderRole')
		left join [dbo].application_oidc_configuration d
		on(d.c_application_id=a.c_application_id)
		left join [dbo].microm_application_certificates f
		on(f.c_application_id=d.c_application_id and f.c_certificate_id=d.c_certificate_id)
		outer apply
		(
			select	frontend_urls='[' + string_agg('"'+replace(rtrim(x.vc_application_url),'"', '\"')+'"', ',') + ']'
			from	[dbo].applications_urls x
			where	x.c_application_id=a.c_application_id
		) e
