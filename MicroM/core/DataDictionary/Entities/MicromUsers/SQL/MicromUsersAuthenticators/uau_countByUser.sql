create or alter proc [dbo].uau_countByUser
    @user_id char(20)
as

select authenticator_count = count(1)
from [dbo].microm_users_authenticators
where c_user_id = @user_id
