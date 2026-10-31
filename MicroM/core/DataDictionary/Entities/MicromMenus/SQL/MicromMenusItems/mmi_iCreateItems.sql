create or alter proc [dbo].mmi_iCreateItems
    @menu_id char(50),
    @json_items varchar(max),
    @webusr varchar(255)
    as

set nocount on

if @@trancount = 0 throw 50001, 'mmi_iCreateItems must be called within a transaction', 1;

if @menu_id is null or trim(@menu_id) = '' throw 51000, 'The parameter @menu_id cannot be null or empty', 1;

declare @now datetime = getdate(), @login sysname = original_login()

declare @items table 
(
    item_id char(50),
    parent_id char(50),
    item_path varchar(max),
    item_name varchar(255)
)

insert  @items (item_id, parent_id, item_path, item_name)
select  i.item_id
        , nullif(i.parent_id, '')
        , i.item_path, i.item_name
from    openjson(coalesce(@json_items, '[]')) 
        with (
            item_id char(50) '$.item_id',
            parent_id char(50) '$.parent_id',
            item_path varchar(max) '$.item_path',
            item_name varchar(255) '$.item_name'
        ) i
where   not exists 
        (
            select  1 
            from    [dbo].microm_menus_items
            where   c_menu_id = @menu_id and c_menu_item_id = i.item_id
        )

if  exists (select 1 from @items where item_id is null or trim(item_id) = '')
    throw 51000, 'The menu item ID cannot be null or empty', 1;

if  exists 
    (
        select  1
        from    @items i
        join    [dbo].microm_menus_items m 
                on m.c_menu_id = @menu_id and m.vc_menu_item_path = i.item_path
    ) 
    or exists 
    (
        select item_path from @items group by item_path having count(*) > 1
    )
    throw 51000, 'The are menu item path that already exists', 1;

insert  [dbo].microm_menus_items
select  @menu_id
        , item_id
        , case when parent_id is null then null else @menu_id end, parent_id
        , item_path
        , item_name
        , @now
        , @now
        , @webusr
        , @webusr
        , @login
        , @login
from    @items
