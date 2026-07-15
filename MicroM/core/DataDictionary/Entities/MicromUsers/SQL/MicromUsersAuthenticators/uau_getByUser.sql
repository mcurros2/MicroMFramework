create or alter proc [dbo].uau_getByUser
    @user_id char(20)
as

select  user_id = rtrim(c_user_id)
        , authenticator_id = rtrim(c_authenticator_id)
        , authenticator_name = vc_authenticator_name
        , totp_secret = vc_totp_secret
from    [dbo].microm_users_authenticators
where   c_user_id = @user_id
order by vc_authenticator_name, c_authenticator_id
