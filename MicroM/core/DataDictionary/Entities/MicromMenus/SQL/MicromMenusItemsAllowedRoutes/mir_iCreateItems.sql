create or alter proc [dbo].mir_iCreateItems
    @menu_id char(50),
    @json_items varchar(max),
    @webusr varchar(255)
    as

set nocount on

if @@trancount = 0 throw 50001, 'mir_iCreateItems must be called within a transaction', 1;
if @menu_id is null or trim(@menu_id) = '' throw 51000, 'The parameter @menu_id cannot be null or empty', 1;

declare @now datetime = getdate(), @login sysname = original_login(),
        @seed bigint, @inserted_routes int, @inserted_links int

declare @items table (item_id char(50), route_path varchar(2048))

declare @new_routes table (route_path varchar(2048))

insert  @items
select  distinct 
        i.item_id
        , convert(varchar(2048), r.value)
from    openjson(coalesce(@json_items, '[]')) 
        with 
        (
            item_id char(50) '$.item_id',
            routes nvarchar(max) '$.routes' as json
        ) i
cross apply openjson(i.routes) r

if exists (select 1 from @items where item_id is null or trim(item_id) = '')
    throw 51000, 'The menu item ID cannot be null or empty', 1;

insert  @new_routes
select  distinct 
        i.route_path
from    @items i
where   not exists 
        (
            select  1 
            from    [dbo].microm_routes
            where   vc_route_path = i.route_path
        )

if exists (select 1 from @new_routes)
begin
    select  @seed = bi_lastnumber
    from    [dbo].numbering with (rowlock, holdlock, updlock)
    where   c_object_id = 'mro'

    if @seed is null throw 51000, 'The numbering row for mro was not found', 1;

    insert  [dbo].microm_routes
    select  right('0000000000' + convert(varchar(20), @seed + row_number() over (order by route_path)), 10)
            , route_path
            , @now, @now, @webusr, @webusr, @login, @login
    from    @new_routes

    set     @inserted_routes = @@rowcount

    update  [dbo].numbering
    set     bi_lastnumber = @seed + @inserted_routes
    where   c_object_id = 'mro'
end

insert  [dbo].microm_menus_items_allowed_routes
select  @menu_id
        , i.item_id
        , r.c_route_id
        , @now, @now, @webusr, @webusr, @login, @login
from    @items i
join    [dbo].microm_routes r 
        on r.vc_route_path = i.route_path
where   not exists 
        (
            select  1 
            from    [dbo].microm_menus_items_allowed_routes
            where   c_menu_id = @menu_id and c_menu_item_id = i.item_id and c_route_id = r.c_route_id
        )

set @inserted_links = @@rowcount

if @inserted_links > 0
    update [dbo].microm_menus set dt_last_route_updated = @now where c_menu_id = @menu_id
