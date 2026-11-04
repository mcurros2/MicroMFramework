create or alter proc [dbo].obj_InitializeObjects
    @json_objects varchar(max),
    @webusr varchar(255)
    as

set nocount on

declare @now datetime = getdate(), @login sysname = original_login()

declare @objects table
(
    object_id char(20),
    table_name varchar(255),
    autonum bit
)

declare @categories table (object_id char(20), category_id char(20))

declare @status table (object_id char(20), status_id char(20))

insert  @objects (object_id, table_name, autonum)
select  o.object_id
        , o.table_name
        , o.autonum
from    openjson(coalesce(@json_objects, '[]'))
        with (
            object_id char(20) '$.object_id',
            table_name varchar(255) '$.table_name',
            autonum bit '$.autonum'
        ) o

insert  @categories (object_id, category_id)
select  distinct
        o.object_id
        , convert(char(20), c.value)
from    openjson(coalesce(@json_objects, '[]'))
        with
        (
            object_id char(20) '$.object_id',
            categories nvarchar(max) '$.categories' as json
        ) o
cross apply openjson(o.categories) c

insert  @status (object_id, status_id)
select  distinct
        o.object_id
        , convert(char(20), s.value)
from    openjson(coalesce(@json_objects, '[]'))
        with
        (
            object_id char(20) '$.object_id',
            status nvarchar(max) '$.status' as json
        ) o
cross apply openjson(o.status) s

if  exists (select 1 from @objects where object_id is null or trim(object_id) = '')
    throw 51000, 'The object ID cannot be null or empty', 1;

begin try
    begin tran

    insert  [dbo].objects
    select  o.object_id
            , o.object_id
            , o.table_name
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
    from    @objects o
    where   not exists
            (
                select  1
                from    [dbo].objects
                where   c_object_id = o.object_id
            )

    insert  [dbo].numbering
    select  o.object_id
            , 0
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
    from    @objects o
    where   o.autonum = 1
            and not exists
            (
                select  1
                from    [dbo].numbering
                where   c_object_id = o.object_id
            )

    insert  [dbo].objects_categories
    select  c.object_id
            , c.category_id
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
    from    @categories c
    where   not exists
            (
                select  1
                from    [dbo].objects_categories
                where   c_object_id = c.object_id and c_category_id = c.category_id
            )

    insert  [dbo].objects_status
    select  s.object_id
            , s.status_id
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
    from    @status s
    where   not exists
            (
                select  1
                from    [dbo].objects_status
                where   c_object_id = s.object_id and c_status_id = s.status_id
            )

    commit tran
    select 0, 'OK'
end try
begin catch
    if @@trancount > 0 rollback;
    throw;
end catch
