create or alter proc eqc_get
        @email_configuration_id Char(20)
        as

select  [c_email_configuration_id] = rtrim(a.c_email_configuration_id)
        , a.vc_smtp_host
        , a.i_smtp_port
        , a.vc_user_name
        , a.vc_password
        , a.bt_use_ssl
        , a.vc_default_sender_email
        , a.vc_default_sender_name
        , b.vc_template_subject /* fake column vc_template_subject */
        , b.vc_template_body /* fake column vc_template_body */
        , a.dt_inserttime
        , a.dt_lu
        , a.vc_webinsuser
        , a.vc_webluuser
        , a.vc_insuser
        , a.vc_luuser
from    [email_service_configuration] a
        left join email_service_templates b
		on(b.c_email_template_id='RECOVERY')
where   a.c_email_configuration_id = @email_configuration_id