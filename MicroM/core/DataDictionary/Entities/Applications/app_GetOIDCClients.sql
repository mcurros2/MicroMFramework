create or alter proc app_GetOIDCClients	as

select	[c_application_id] = rtrim(a.c_application_id)
		, [c_client_app_id] = rtrim(a.c_client_app_id)
		, [vc_url_sso_frontchannel_logout] = a.vc_url_sso_frontchannel_logout
		, [vc_url_sso_backchannel_logout] = a.vc_url_sso_backchannel_logout
		, [vc_url_client_jwks] = a.vc_url_client_jwks
		, [vc_url_authorized_redirects] = e.authorized_urls
		, [vc_certificate_unique_id] = a.vc_certificate_unique_id
		, [vc_apikey] = b.vc_apikey
		, [vc_secret] = b.vc_secret
		, [vc_oidc_subject_pepper] = a.vc_oidc_subject_pepper
from	applications c
		join applications_cat d
		on(d.c_application_id=c.c_application_id and d.c_category_id='IdentityProviderRole' and d.c_categoryvalue_id = 'IDPServer')
		join application_oidc_clients a
		on(a.c_application_id=c.c_application_id)
		left join microm_application_api_keys b
		on(b.c_application_id=a.c_application_id and b.c_api_key_id=a.c_api_key_id)
		outer apply
		(
			select	authorized_urls='[' + string_agg('"'+replace(rtrim(x.vc_authorized_url),'"', '\"')+'"', ',') + ']'
			from	application_oidc_clients_authorized_urls x
			where	x.c_application_id=a.c_application_id
					and x.c_client_app_id=a.c_client_app_id
		) e
