create or alter proc [dbo].app_GetADConfiguration as

select	[ADConfigurationID] = rtrim(a.c_ad_configuration_id)
		, [ApplicationID] = rtrim(a.c_application_id)
		, [ADDomain] = a.vc_ad_domain
		, [ADUserPrincipalDomain] = a.vc_user_principal_domain
		, [ADContainer] = a.vc_ad_container
		, [ADServerIP] = a.vc_ad_server_ip
		, [ADUser] = a.vc_ad_user
		, [ADPassword] = a.vc_ad_password
		, [CreateUserOnLogin] = a.bt_create_user_on_login
		, [DefaultUserGroupID] = a.c_default_user_group_id
from	[dbo].application_ad_configuration a