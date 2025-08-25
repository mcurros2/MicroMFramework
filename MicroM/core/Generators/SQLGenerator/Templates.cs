namespace MicroM.Generators.SQLGenerator
{
    /// <summary>
    /// Provides reusable SQL statement templates with token placeholders that
    /// are substituted when generating stored procedures and other scripts.
    /// </summary>
    internal class Templates
    {
        /// <summary>
        /// Template for constructing a LIKE filter. Replace <c>{LIKE_CLAUSE}</c>
        /// with the comparison expressions evaluated for each token returned by
        /// <c>sys_tfLike(@like)</c>.
        /// </summary>
        internal const string LIKE_TEMPLATE =
@"
        not exists (
            select  1
            from    sys_tfLike(@like) l
            where   not 
            (
                {LIKE_CLAUSE}
            )
        )
";
        /// <summary>
        /// Template for a browse/view stored procedure. Replace
        /// <c>{CREATE}</c>, <c>{MNEO}</c>, <c>{PARMS_DECLARATION}</c>,
        /// <c>{VIEW_COLUMNS}</c>, <c>{TABLE_NAME}</c>, <c>{CATEGORIES_JOIN}</c>
        /// and <c>{WHERE_CLAUSE}</c> with generated values.
        /// </summary>
        internal const string VIEW_TEMPLATE =
@"
{CREATE} proc {MNEO}_brwStandard
        {PARMS_DECLARATION}
        as

select  {VIEW_COLUMNS}
from    {TABLE_NAME}{CATEGORIES_JOIN}
where   {WHERE_CLAUSE}
";

        /// <summary>
        /// Template for a drop stored procedure that removes a record and its
        /// related category and status rows. Placeholders:
        /// <c>{CREATE}</c>, <c>{MNEO}</c>, <c>{PARMS_DECLARATION}</c>,
        /// <c>{CATEGORIES_DELETE}</c>, <c>{STATUS_DELETE}</c>,
        /// <c>{TABLE_NAME}</c>, <c>{WHERE_CLAUSE}</c>.
        /// </summary>
        internal const string DROP_TEMPLATE =
@"
{CREATE} proc {MNEO}_drop
        {PARMS_DECLARATION}
        as

begin try

    begin tran

    {CATEGORIES_DELETE}
    {STATUS_DELETE}
    delete  {TABLE_NAME}
    where   {WHERE_CLAUSE}

    commit tran
    select  0, 'OK'

end try
begin catch

    if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;

end catch
";

        /// <summary>
        /// Internal drop stored procedure used inside an existing transaction.
        /// Accepts <c>@result</c> and <c>@msg</c> output parameters. Uses the
        /// same placeholders as <c>DROP_TEMPLATE</c>.
        /// </summary>
        internal const string IDROP_TEMPLATE =
@"
{CREATE} proc {MNEO}_idrop
        {PARMS_DECLARATION}
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 51000, '{MNEO}_idrop must be called within a transaction', 1

begin try

    {CATEGORIES_DELETE}
    {STATUS_DELETE}
    delete  {TABLE_NAME}
    where   {WHERE_CLAUSE}

    select  @result=0, @msg='OK'

end try
begin catch

    throw;

end catch
";

        /// <summary>
        /// Drop wrapper that begins a transaction and calls the internal drop
        /// procedure. Placeholders include <c>{CREATE}</c>, <c>{MNEO}</c>,
        /// <c>{PARMS_DECLARATION}</c> and <c>{PARMS}</c>.
        /// </summary>
        internal const string DROP_CALLS_IDROP_TEMPLATE =
@"
{CREATE} proc {MNEO}_drop
        {PARMS_DECLARATION}
        as

    declare @result int, @msg varchar(255)

begin try

    begin tran

    exec    {MNEO}_idrop
            {PARMS}
            , @result = @result OUT
            , @msg = @msg OUT

    if @result<>0
    begin
        rollback
        select  @result, @msg
        return
    end
    
    commit tran
    select  @result, @msg

end try
begin catch

    if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;
end catch
";

        /// <summary>
        /// Template for a lookup stored procedure returning a description
        /// column. Placeholders: <c>{CREATE}</c>, <c>{MNEO}</c>,
        /// <c>{PARMS_DECLARATION}</c>, <c>{DESC_COLUMN}</c>,
        /// <c>{TABLE_NAME}</c>, <c>{WHERE_CLAUSE}</c>.
        /// </summary>
        internal const string LOOKUP_TEMPLATE =
@"
{CREATE} proc {MNEO}_lookup
        {PARMS_DECLARATION}
        as

select  {DESC_COLUMN}
from    {TABLE_NAME}
where   {WHERE_CLAUSE}
";

        /// <summary>
        /// Selects category values into a JSON array. Replace
        /// <c>{CATEGORY_PARM}</c>, <c>{CATEGORIES_TABLE}</c> and
        /// <c>{WHERE_CLAUSE}</c> as needed.
        /// </summary>
        internal const string JSON_CATEGORY_GET_TEMPLATE =
@"
select  {CATEGORY_PARM} = '[' + STRING_AGG('""'+replace(RTRIM(c_categoryvalue_id), '""','\""')+'""', ',') + ']'
from    {CATEGORIES_TABLE}
where   {WHERE_CLAUSE}
";


        /// <summary>
        /// Template for a get stored procedure returning a single row along
        /// with JSON category information. Placeholders include
        /// <c>{CREATE}</c>, <c>{MNEO}</c>, <c>{PARMS_DECLARATION}</c>,
        /// <c>{JSON_PARMS_DECLARATION}</c>, <c>{JSON_CATEGORIES_GET}</c>,
        /// <c>{GET_VALUES}</c>, <c>{TABLE_NAME}</c>, <c>{CATEGORIES_JOIN}</c>,
        /// and <c>{WHERE_CLAUSE}</c>.
        /// </summary>
        internal const string GET_TEMPLATE =
@"
{CREATE} proc {MNEO}_get
        {PARMS_DECLARATION}
        as

{JSON_PARMS_DECLARATION}
{JSON_CATEGORIES_GET}
select  {GET_VALUES}
from    {TABLE_NAME}{CATEGORIES_JOIN}
where   {WHERE_CLAUSE}
";

        /// <summary>
        /// Concurrency check snippet for update procedures. Replaces the
        /// transaction with a rollback when the last-update timestamp does not
        /// match. Used inside <c>UPDATE_TEMPLATE</c>.
        /// </summary>
        internal const string UPDATE_LU_CONTROL_TEMPLATE =
@"
    if @cu<>@lu or @lu is null 
    begin
        commit tran
        select 4, 'Record changed'
        return
    end

";

        /// <summary>
        /// Concurrency check for inline update procedures returning
        /// <c>@result</c> and <c>@msg</c>. Used in <c>IUPDATE_TEMPLATE</c>.
        /// </summary>
        internal const string IUPDATE_LU_CONTROL_TEMPLATE =
@"
    if @cu<>@lu or @lu is null 
    begin
        select @result = 4, @msg = 'Record changed'
        return
    end

";

        /// <summary>
        /// Update statement for the main table. Replace <c>{TABLE_NAME}</c>,
        /// <c>{UPDATE_VALUES}</c> and <c>{WHERE_CLAUSE}</c> with generated
        /// values.
        /// </summary>
        internal const string UPDATE_CLAUSE_TEMPLATE =
@"
    update  {TABLE_NAME}
    set     {UPDATE_VALUES}
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   {WHERE_CLAUSE}
";

        /// <summary>
        /// Parses a JSON array of category values into a temporary table and
        /// ensures missing category entries are inserted. Placeholders:
        /// <c>{CATEGORY_TEMP_TABLE}</c>, <c>{CATEGORY_PARM}</c>,
        /// <c>{CATEGORY}</c>.
        /// </summary>
        internal const string JSON_CATEGORIES_PARSE_TEMPLATE =
        @"
    create table {CATEGORY_TEMP_TABLE} (jsoncategory_id char(20), category_desc varchar(max))

    IF {CATEGORY_PARM} IS NOT NULL
    BEGIN
        insert  {CATEGORY_TEMP_TABLE}
        select  isnull(rtrim(b.c_categoryvalue_id),CONVERT(VARCHAR(20), CONVERT(BIGINT, CHECKSUM(category_desc)) & 0xFFFFFFFF))
                , category_desc
        from    openjson({CATEGORY_PARM}) WITH (category_desc varchar(max) '$') a
                left join categories_values b
                on(b.c_categoryvalue_id=a.category_desc and b.c_category_id={CATEGORY})
    END

    -- insert new categories first
    insert  [categories_values]
    select  {CATEGORY}
            , a.jsoncategory_id
            , a.category_desc
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
    from    {CATEGORY_TEMP_TABLE} a
    where   not exists (
                                select  *
                                from    [categories_values]
                                where   c_category_id = {CATEGORY}
                                                and c_categoryvalue_id = a.jsoncategory_id
                                )
";


        /// <summary>
        /// Inserts category rows from the JSON temp table when the parameter is
        /// not null. Placeholders: <c>{CATEGORY_PARM}</c>,
        /// <c>{CATEGORIES_TABLE}</c>, <c>{INSERT_VALUES}</c>,
        /// <c>{CATEGORY_TEMP_TABLE}</c>.
        /// </summary>
        internal const string INSERT_JSON_CAT_TEMPLATE =
        @"
        if ({CATEGORY_PARM} is not null)
        begin

            insert  {CATEGORIES_TABLE}
            select  {INSERT_VALUES}
                    , @now
                    , @now
                    , @webusr
                    , @webusr
                    , @login
                    , @login
            from    {CATEGORY_TEMP_TABLE}

        end
";

        /// <summary>
        /// Synchronises category rows based on the JSON temp table. Deletes
        /// missing values and inserts new ones. Placeholders:
        /// <c>{CATEGORIES_TABLE}</c>, <c>{WHERE_CLAUSE}</c>,
        /// <c>{INSERT_VALUES}</c>, <c>{CATEGORY_TEMP_TABLE}</c>.
        /// </summary>
        internal const string UPDATE_JSON_CAT_TEMPLATE =
        @"
    delete  {CATEGORIES_TABLE}
    WHERE   {WHERE_CLAUSE}
            and c_categoryvalue_id not in(SELECT jsoncategory_id FROM {CATEGORY_TEMP_TABLE})

    INSERT {CATEGORIES_TABLE}
    SELECT  {INSERT_VALUES}
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
    FROM    {CATEGORY_TEMP_TABLE}
    WHERE   jsoncategory_id NOT IN
            (
                SELECT  c_categoryvalue_id
                FROM    {CATEGORIES_TABLE}
                where   {WHERE_CLAUSE}
            )
";


        /// <summary>
        /// Full update stored procedure. Handles insert when the record does
        /// not exist, performs concurrency checks and updates categories and
        /// status. Uses numerous placeholders such as <c>{CREATE}</c>,
        /// <c>{MNEO}</c>, <c>{PARMS_DECLARATION}</c>, <c>{PARMS_VALIDATION}</c>,
        /// <c>{NULLIF_CHECKS}</c>, <c>{JSON_CATEGORIES}</c>,
        /// <c>{TABLE_NAME}</c>, <c>{WHERE_CLAUSE}</c>, <c>{AUTONUM}</c>,
        /// <c>{INSERT_VALUES}</c>, <c>{JSON_CATEGORIES_INSERT}</c>,
        /// <c>{CATEGORIES_INSERT}</c>, <c>{STATUS_INSERT}</c>,
        /// <c>{AUTONUM_RETURN}</c>, <c>{UPDATE_LU_CONTROL}</c>,
        /// <c>{UPDATE_CLAUSE}</c>, <c>{JSON_CATEGORIES_UPDATE}</c>,
        /// <c>{CATEGORIES_UPDATE}</c> and <c>{STATUS_UPDATE}</c>.
        /// </summary>
        internal const string UPDATE_TEMPLATE =
@"
{CREATE} proc {MNEO}_update
        {PARMS_DECLARATION}
        as

{PARMS_VALIDATION}
{NULLIF_CHECKS}

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    begin tran
    {JSON_CATEGORIES}
    select  @cu=dt_lu
    from    {TABLE_NAME} with (rowlock, holdlock, updlock)
    where   {WHERE_CLAUSE}

    if @cu is null
    begin
        {AUTONUM}
        insert  {TABLE_NAME}
        values
            (
            {INSERT_VALUES}
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )
        {JSON_CATEGORIES_INSERT}
        {CATEGORIES_INSERT}
        {STATUS_INSERT}
        commit tran
        {AUTONUM_RETURN}
        return
    end
    
{UPDATE_LU_CONTROL}
{UPDATE_CLAUSE}
    {JSON_CATEGORIES_UPDATE}
    {CATEGORIES_UPDATE}
    {STATUS_UPDATE}
    commit tran
    select 0, 'OK'
    
end try
begin catch

    if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;

end catch
";

        /// <summary>
        /// Inline update stored procedure intended to run within an existing
        /// transaction and returning <c>@result</c> and <c>@msg</c> outputs.
        /// Shares placeholders with <c>UPDATE_TEMPLATE</c>.
        /// </summary>
        internal const string IUPDATE_TEMPLATE =
@"
{CREATE} proc {MNEO}_iupdate
        {PARMS_DECLARATION}
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 50001, '{MNEO}_iupdate must be called within a transaction', 1

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()
    {JSON_CATEGORIES}
    select  @cu=dt_lu
    from    {TABLE_NAME} with (rowlock, holdlock, updlock)
    where   {WHERE_CLAUSE}

    if @cu is null
    begin
        {AUTONUM}
        insert  {TABLE_NAME}
        values
            (
            {INSERT_VALUES}
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )
        {JSON_CATEGORIES_INSERT}
        {CATEGORIES_INSERT}
        {STATUS_INSERT}
        {AUTONUM_RETURN}
        return
    end
    
{UPDATE_LU_CONTROL}
{UPDATE_CLAUSE}
    {JSON_CATEGORIES_UPDATE}
    {CATEGORIES_UPDATE}
    {STATUS_UPDATE}
    select @result = 0, @msg = 'OK'
    
end try
begin catch

    throw;

end catch
";

        /// <summary>
        /// Wrapper update stored procedure that validates parameters, begins a
        /// transaction and calls the internal update procedure.
        /// Placeholders: <c>{CREATE}</c>, <c>{MNEO}</c>,
        /// <c>{PARMS_DECLARATION}</c>, <c>{PARMS_VALIDATION}</c>,
        /// <c>{NULLIF_CHECKS}</c>, <c>{PARMS}</c>.
        /// </summary>
        internal const string UPDATE_CALLS_IUPDATE_TEMPLATE =
@"
{CREATE} proc {MNEO}_update
        {PARMS_DECLARATION}
        as

{PARMS_VALIDATION}
{NULLIF_CHECKS}

begin try

    declare @result int, @msg varchar(255)

    begin tran

    exec    {MNEO}_iupdate
            {PARMS}
            , @result = @result OUT
            , @msg = @msg OUT

    if @result not in(0,15)
    begin
        rollback
        select  @result, @msg
        return
    end

    commit tran
    select  @result, @msg

end try
begin catch

    if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;

end catch
";

        /// <summary>
        /// Inserts a category row only when the category parameter is not
        /// null. Placeholders: <c>{CATEGORY_PARM}</c>, <c>{CATEGORIES_TABLE}</c>,
        /// <c>{INSERT_VALUES}</c>.
        /// </summary>
        internal const string INSERT_CATEGORY_TEMPLATE_NULL =
@"
        if ({CATEGORY_PARM} is not null)
        begin

            insert  {CATEGORIES_TABLE}
            values
                (
                {INSERT_VALUES}
                , @now
                , @now
                , @webusr
                , @webusr
                , @login
                , @login
                )

        end
";

        /// <summary>
        /// Inserts a category row without checking for null. Placeholders:
        /// <c>{CATEGORIES_TABLE}</c>, <c>{INSERT_VALUES}</c>.
        /// </summary>
        internal const string INSERT_CATEGORY_TEMPLATE =
@"
        insert  {CATEGORIES_TABLE}
        values  
            (
            {INSERT_VALUES}
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )
";

        /// <summary>
        /// Deletes category rows when the category parameter is null.
        /// Placeholders: <c>{CATEGORY_PARM}</c>, <c>{CATEGORIES_TABLE}</c>,
        /// <c>{WHERE_CLAUSE}</c>.
        /// </summary>
        internal const string DELETE_CATEGORY_NULL_TEMPLATE =
@"
    if ({CATEGORY_PARM} is null)
    begin

        delete  {CATEGORIES_TABLE}
        where   {WHERE_CLAUSE}
        
    end

";

        /// <summary>
        /// Inserts, updates or deletes a single category row based on the
        /// supplied parameter. Uses placeholders <c>{CATEGORY_DELETE_NULL}</c>,
        /// <c>{CATEGORIES_TABLE}</c>, <c>{WHERE_CLAUSE}</c>,
        /// <c>{CATEGORY_PARM}</c>, <c>{INSERT_VALUES}</c>,
        /// <c>{UPDATE_VALUES}</c>.
        /// </summary>
        internal const string UPDATE_CATEGORY_TEMPLATE =
@"
    {CATEGORY_DELETE_NULL}
    if not exists (
            select  *
            from    {CATEGORIES_TABLE}
            where   {WHERE_CLAUSE}
            )
    begin

        if {CATEGORY_PARM} is not null
        begin
            insert  {CATEGORIES_TABLE}
            values  
                (
                {INSERT_VALUES}
                , @now
                , @now
                , @webusr
                , @webusr
                , @login
                , @login
                )
        end

    end
    else
    begin

        update  {CATEGORIES_TABLE}
        set     {UPDATE_VALUES}
                , vc_webluuser = @webusr
                , vc_luuser = @login
                , dt_lu = @now
        where   {WHERE_CLAUSE}

    end
";

        /// <summary>
        /// Deletes category rows unconditionally. Placeholders:
        /// <c>{CATEGORIES_TABLE}</c> and <c>{WHERE_CLAUSE}</c>.
        /// </summary>
        internal const string DELETE_CATEGORY_TEMPLATE =
@"
    delete  {CATEGORIES_TABLE}
    where   {WHERE_CLAUSE}
";

        /// <summary>
        /// Inserts initial status values for a new record. Replace
        /// <c>{STATUS_TABLE}</c>, <c>{INSERT_VALUES}</c> and <c>{MNEO}</c>.
        /// </summary>
        internal const string INSERT_STATUS_TEMPLATE =
@"
        insert  {STATUS_TABLE}
        select  {INSERT_VALUES}
                , @now
                , @now
                , @webusr
                , @webusr
                , @login
                , @login
        from    status_values a
                join objects_status b
                on(b.c_status_id = a.c_status_id)
                join [objects] c
                on(c.c_object_id = b.c_object_id)
        where   c.c_mneo_id = {MNEO} and
                a.bt_initial_value = 1
";

        /// <summary>
        /// Deletes status rows. Placeholders: <c>{STATUS_TABLE}</c>,
        /// <c>{WHERE_CLAUSE}</c>.
        /// </summary>
        internal const string DELETE_STATUS_TEMPLATE =
@"
    delete  {STATUS_TABLE}
    where   {WHERE_CLAUSE}
";

        /// <summary>
        /// Updates the status value when it changes. Placeholders:
        /// <c>{STATUS_TABLE}</c>, <c>{UPDATE_VALUES}</c>, <c>{WHERE_CLAUSE}</c>,
        /// <c>{STATUS_PARM}</c>.
        /// </summary>
        internal const string UPDATE_STATUS_TEMPLATE =
@"
    update  {STATUS_TABLE}
    set     {UPDATE_VALUES}
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   {WHERE_CLAUSE}
            and c_statusvalue_id <> {STATUS_PARM}
";

    }

}
