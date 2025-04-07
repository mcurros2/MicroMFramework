create or alter function emq_fReplaceTags(@template varchar(max), @tags varchar(max)) returns nvarchar(max) as
begin
    if ISJSON(@tags) <> 1
    begin
        -- force error as throw is not allowed in functions
        return cast('Invalid JSON format' as int);
    end

    -- Expected format
    if exists (
        select 1 
        from openjson(@tags)
        with (
            tag varchar(255),
            value varchar(255)
        )
        where tag is null or value is null
    )
    begin
        -- force error as throw is not allowed in functions
        return cast('Invalid JSON format. Expected [{tag: string, value: string}, ...]' as int);
    end

    declare @FinalResult varchar(max) = @template;

    declare @Replacements table (tag varchar(255), value varchar(255));
    insert into @Replacements(tag, value)
    select tag, value
    from openjson(@tags) 
    with (
        tag varchar(255),
        value varchar(255)
    );

    declare @CurrentTag varchar(255);
    declare @CurrentValue varchar(255);

    while exists (select 1 from @Replacements)
    begin
        select top 1 @CurrentTag = tag, @CurrentValue = value from @Replacements;

        set @FinalResult = replace(@FinalResult, @CurrentTag, @CurrentValue);

        delete from @Replacements where tag = @CurrentTag;
    end

    return @FinalResult
end