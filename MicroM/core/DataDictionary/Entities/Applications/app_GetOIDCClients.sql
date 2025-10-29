create or alter proc app_GetOIDCClients	as

select	[ApplicationID] = rtrim(a.c_application_id)
		, [ClientAPPID] = rtrim(a.c_client_app_id)
		, [URLFrontChannelLogout] = a.vc_url_sso_frontchannel_logout
		, [URLBackchannelLogout] = a.vc_url_sso_backchannel_logout
		, [URLClientJWKS] = a.vc_url_client_jwks
		, [URLAuthorizedRedirects] = e.authorized_urls
		, [CertificateUniqueID] = a.vc_certificate_unique_id
		, [APIKey] = b.vc_apikey
		, [APISecret] = b.vc_secret
		, [OIDCSubjectPepper] = a.vc_oidc_subject_pepper
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
