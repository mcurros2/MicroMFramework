create or alter proc [dbo].cat_InitializeCategories
    @json_categories varchar(max),
    @webusr varchar(255)
    as

set nocount on

declare @now datetime = getdate(), @login sysname = original_login()

declare @categories table
(
    category_id char(20),
    description varchar(255)
)

declare @values table
(
    category_id char(20),
    value_id char(20),
    description varchar(255)
)

insert  @categories (category_id, description)
select  c.category_id
        , c.description
from    openjson(coalesce(@json_categories, '[]'))
        with (
            category_id char(20) '$.category_id',
            description varchar(255) '$.description'
        ) c

insert  @values (category_id, value_id, description)
select  c.category_id
        , v.value_id
        , v.description
from    openjson(coalesce(@json_categories, '[]'))
        with
        (
            category_id char(20) '$.category_id',
            category_values nvarchar(max) '$.values' as json
        ) c
cross apply openjson(c.category_values)
        with (
            value_id char(20) '$.value_id',
            description varchar(255) '$.description'
        ) v

if  exists (select 1 from @categories where category_id is null or trim(category_id) = '')
    throw 51000, 'The category ID cannot be null or empty', 1;

if  exists (select 1 from @values where value_id is null or trim(value_id) = '')
    throw 51000, 'The category value ID cannot be null or empty', 1;

begin try
    begin tran

    insert  [dbo].categories
    select  c.category_id
            , c.description
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
                from    [dbo].categories
                where   c_category_id = c.category_id
            )

    insert  [dbo].categories_values
    select  v.category_id
            , v.value_id
            , v.description
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
    from    @values v
    where   not exists
            (
                select  1
                from    [dbo].categories_values
                where   c_category_id = v.category_id and c_categoryvalue_id = v.value_id
            )

    commit tran
    select 0, 'OK'
end try
begin catch
    if @@trancount > 0 rollback;
    throw;
end catch
