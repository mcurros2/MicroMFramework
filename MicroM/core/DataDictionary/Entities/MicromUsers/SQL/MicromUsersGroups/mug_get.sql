create or alter proc mug_get
        @user_group_id Char(20)
        as

declare @members varchar(max) = '[]'

select  @members = '[' + STRING_AGG('"'+replace(RTRIM(c_user_id), '"','\"')+'"', ',') + ']'
from    microm_users_groups_members
where   c_user_group_id = @user_group_id

select  [c_user_group_id] = rtrim(a.c_user_group_id)
        , a.vc_user_group_name
        , @members /* fake column vc_group_members */
        , a.dt_inserttime
        , a.dt_lu
        , a.vc_webinsuser
        , a.vc_webluuser
        , a.vc_insuser
        , a.vc_luuser
from    [microm_users_groups] a
where   a.c_user_group_id = @user_group_id