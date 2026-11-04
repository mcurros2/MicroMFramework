create or alter proc [dbo].sta_InitializeStatus
    @json_status varchar(max),
    @webusr varchar(255)
    as

set nocount on

declare @now datetime = getdate(), @login sysname = original_login()

declare @status table
(
    status_id char(20),
    description varchar(255)
)

declare @values table
(
    status_id char(20),
    value_id char(20),
    description varchar(255),
    initial_value bit
)

insert  @status (status_id, description)
select  s.status_id
        , s.description
from    openjson(coalesce(@json_status, '[]'))
        with (
            status_id char(20) '$.status_id',
            description varchar(255) '$.description'
        ) s

insert  @values (status_id, value_id, description, initial_value)
select  s.status_id
        , v.value_id
        , v.description
        , v.initial_value
from    openjson(coalesce(@json_status, '[]'))
        with
        (
            status_id char(20) '$.status_id',
            status_values nvarchar(max) '$.values' as json
        ) s
cross apply openjson(s.status_values)
        with (
            value_id char(20) '$.value_id',
            description varchar(255) '$.description',
            initial_value bit '$.initial_value'
        ) v

if  exists (select 1 from @status where status_id is null or trim(status_id) = '')
    throw 51000, 'The status ID cannot be null or empty', 1;

if  exists (select 1 from @values where value_id is null or trim(value_id) = '')
    throw 51000, 'The status value ID cannot be null or empty', 1;

begin try
    begin tran

    insert  [dbo].status
    select  s.status_id
            , s.description
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
                from    [dbo].status
                where   c_status_id = s.status_id
            )

    insert  [dbo].status_values
    select  v.status_id
            , v.value_id
            , v.description
            , v.initial_value
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
                from    [dbo].status_values
                where   c_status_id = v.status_id and c_statusvalue_id = v.value_id
            )

    commit tran
    select 0, 'OK'
end try
begin catch
    if @@trancount > 0 rollback;
    throw;
end catch
