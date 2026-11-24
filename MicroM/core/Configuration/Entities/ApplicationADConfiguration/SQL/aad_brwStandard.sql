create or alter proc [dbo].aad_brwStandard
        @ad_configuration_id Char(20)
        , @application_id Char(20)
        , @like VarChar(max)
        , @d Char(1)
        as

select  [Config Id] = rtrim(a.c_ad_configuration_id)
        , [Application Id] = rtrim(a.c_application_id)
        , [Ad Domain] = a.vc_ad_domain
        , [User Principal Domain] = a.vc_user_principal_domain
        , [Ad Container] = a.vc_ad_container
        , [Ad Server Ip] = a.vc_ad_server_ip
        , [Ad User] = a.vc_ad_user
        , [Ad Password] = a.vc_ad_password
        , [Create User On Login] = a.bt_create_user_on_login
        , [Default User Group Id] = rtrim(a.c_default_user_group_id)
from    [dbo].[application_ad_configuration] a
where   
        not exists (
            select  1
            from    sys_tfLike(@like) l
            where   not 
            (
                isnull(rtrim(a.c_ad_configuration_id),'') like l.phrase
                or isnull(rtrim(a.c_application_id),'') like l.phrase
                or isnull(rtrim(a.vc_ad_domain),'') like l.phrase
                or isnull(rtrim(a.vc_user_principal_domain),'') like l.phrase
                or isnull(rtrim(a.vc_ad_container),'') like l.phrase
                or isnull(rtrim(a.vc_ad_server_ip),'') like l.phrase
                or isnull(rtrim(a.vc_ad_user),'') like l.phrase
                or isnull(rtrim(a.vc_ad_password),'') like l.phrase
                or isnull(rtrim(a.bt_create_user_on_login),'') like l.phrase
                or isnull(rtrim(a.c_default_user_group_id),'') like l.phrase
            )
        )
