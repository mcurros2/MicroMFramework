create or alter proc usr_getUserData @username varchar(255), @user_id char(20), @device_id varchar(255) as

declare @now datetime = getdate(), @groups varchar(max)


select  @groups = '[' + STRING_AGG('"'+replace(RTRIM(c_user_group_id), '"','\"')+'"', ',') + ']'
from    microm_users_groups_members a
		join microm_users b
		on(b.c_user_id=a.c_user_id)
where	b.vc_username=@username
		or b.c_user_id=@user_id

select	[user_id] = rtrim(a.c_user_id)
		, locked  = cast(case when a.dt_locked is not null and a.dt_locked > @now then 1 else 0 end as bit)
		, locked_minutes_remaining = case when a.dt_locked is not null and a.dt_locked > @now then datediff(mi,@now,a.dt_locked) else 0 end
		, pwhash = a.vc_pwhash
		, badlogonattempts = a.i_badlogonattempts
		, email = a.vc_email
		, username = a.vc_username
		, [disabled] = a.bt_disabled
		, refresh_token = b.vc_refreshtoken
		, refresh_expired = cast(case when b.dt_refresh_expiration is not null and b.dt_refresh_expiration>@now then 0 else 1 end as bit)
		, [usertype_id] = rtrim(c.c_categoryvalue_id)
		, [usertype_name] = d.vc_description
		, [user_groups] = @groups
from	microm_users a
		left join microm_users_devices b
		on(b.c_user_id=a.c_user_id and b.c_device_id=@device_id)
		left join 
		(	microm_users_cat c
			join categories_values d
			on(d.c_category_id=c.c_category_id and d.c_categoryvalue_id=c.c_categoryvalue_id)
		)
		on(c.c_user_id=a.c_user_id and c.c_category_id='UserTypes')
where	a.vc_username=@username
		or a.c_user_id=@user_id
