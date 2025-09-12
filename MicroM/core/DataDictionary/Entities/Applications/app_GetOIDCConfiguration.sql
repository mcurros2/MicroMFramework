create or alter proc app_GetOIDCConfiguration as

select	[c_application_id] = rtrim(a.c_application_id)
		, [c_identity_provider_role_id] = rtrim(c.c_categoryvalue_id)
		, [vc_oidc_url_wellknown]=d.vc_url_wellknown
		, [vc_certificate_unique_id]=convert(varchar(2048),e.ui_certificate_guid_id)
		, vb_certificate_blob = e.vb_certificate_blob 
		, vc_certificate_password = e.vc_certificate_password
from	applications a
		join applications_cat c
		on(c.c_application_id=a.c_application_id and c.c_category_id='IdentityProviderRole' and c.c_categoryvalue_id in ('IDPServer', 'IDPClient'))
		join application_oidc_configuration d
		on(d.c_application_id=a.c_application_id)
		left join microm_application_certificates e
		on(e.c_application_id=d.c_application_id and e.c_certificate_id=d.c_certificate_id)

