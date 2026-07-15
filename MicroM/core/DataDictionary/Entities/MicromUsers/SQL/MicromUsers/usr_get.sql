create or alter proc [dbo].usr_get
        @user_id Char(20)
        as

declare @now datetime = getdate(), @groups varchar(max)


select  @groups = '[' + STRING_AGG('"'+replace(RTRIM(c_user_group_id), '"','\"')+'"', ',') + ']'
from    [dbo].microm_users_groups_members
where   c_user_id = @user_id

select  [c_user_id] = rtrim(a.c_user_id)
        , a.vc_username
        , a.vc_email
        , vc_pwhash = null
        , vb_sid = null
        , a.i_badlogonattempts
        , [bt_disabled] = a.bt_disabled
        , a.dt_locked
        , a.dt_last_login
        , a.dt_last_refresh
        , a.bt_totp_enabled
        , vc_recovery_code = null
        , dt_last_recovery = null
        , [c_usertype_id] = rtrim(b.c_categoryvalue_id)
        , [vc_user_groups] = @groups
        , [bt_islocked] = cast(case when a.dt_locked is not null and a.dt_locked > @now then 1 else 0 end as bit)
        , [i_locked_minutes_remaining] = case when a.dt_locked is not null and a.dt_locked > @now then datediff(mi,@now,a.dt_locked) else 0 end
        , vc_password = null
        , a.dt_inserttime
        , a.dt_lu
        , a.vc_webinsuser
        , a.vc_webluuser
        , a.vc_insuser
        , a.vc_luuser
from    [dbo].[microm_users] a
        join [dbo].[microm_users_cat] b
        on(a.c_user_id = b.c_user_id
        and b.c_category_id = 'UserTypes')
where   a.c_user_id = @user_id
