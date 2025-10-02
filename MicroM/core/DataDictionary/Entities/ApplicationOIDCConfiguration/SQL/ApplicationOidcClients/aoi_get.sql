create or alter proc aoi_get
        @application_id Char(20)
        , @client_app_id Char(20)
        as

declare @appurls VarChar(max)

select  @appurls = '[' + STRING_AGG('"'+replace(RTRIM(c_client_app_url_id), '"','\"')+'"', ',') + ']'
from    application_oidc_clients_urls
where   c_application_id = @application_id
        and c_client_app_id = @client_app_id


select  [c_application_id] = rtrim(a.c_application_id)
        , [c_client_app_id] = rtrim(a.c_client_app_id)
        , [c_api_key_id] = null
        , a.vc_url_sso_frontchannel_logout
        , a.vc_url_sso_backchannel_logout
        , a.vc_url_client_jwks
        , a.vc_certificate_unique_id
        , b.vc_apikey
        , b.vc_secret
        , vc_url_authorized_redirects=@appurls /* fake list of c_client_app_url_id */
        , b_change_secret=0 /* fake column b_change_secret */
        , a.dt_inserttime
        , a.dt_lu
        , a.vc_webinsuser
        , a.vc_webluuser
        , a.vc_insuser
        , a.vc_luuser
from    [application_oidc_clients] a
        left join microm_application_api_keys b
        on(b.c_application_id=a.c_application_id and b.c_api_key_id=a.c_api_key_id)
where   a.c_application_id = @application_id
        and a.c_client_app_id = @client_app_id