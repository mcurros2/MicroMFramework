create or alter proc usr_GetEnabledMenus
		@username varchar(255)
		as

declare	@user_id char(20), @usertype_id char(20)

select	@user_id=a.c_user_id
		, @usertype_id=b.c_categoryvalue_id
from	microm_users a
		join microm_users_cat b
		on(b.c_user_id=a.c_user_id and b.c_category_id='UserTypes')
where	a.vc_username=@username

select	distinct
		c_menu_id = rtrim(a.c_menu_id)
		, c_menu_item_id = rtrim(a.c_menu_item_id)
from	microm_users_groups_menus a
		join microm_users_groups_members b
		on(a.c_user_group_id=b.c_user_group_id)
where	b.c_user_id=@user_id
union
select	c_menu_id=rtrim(a.c_menu_id)
		, c_menu_item_id=rtrim(a.c_menu_item_id)
from	microm_menus_items a
where	@usertype_id='ADMIN'