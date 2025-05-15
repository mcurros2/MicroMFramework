create or alter function emq_fReplaceTags(@template varchar(max), @tags varchar(max)) returns nvarchar(max) as
begin
    declare @FinalResult varchar(max) = @template;

    declare @Replacements table (tag varchar(255), value varchar(255));

    -- If it's json and in expected format
    if  ISJSON(@tags) = 1 
        and
        not exists 
        (
            select 1 
            from openjson(@tags)
            with 
            (
                tag varchar(255),
                value varchar(255)
            )
            where tag is null
        )

    begin
        insert into @Replacements(tag, value)
        select tag, value
        from openjson(@tags) 
        with (
            tag varchar(255),
            value varchar(255)
        );
    end

    declare @CurrentTag varchar(255);
    declare @CurrentValue varchar(255);

    while exists (select 1 from @Replacements)
    begin
        select top 1 @CurrentTag = tag, @CurrentValue = isnull(value,'') from @Replacements;

        set @FinalResult = replace(@FinalResult, @CurrentTag, @CurrentValue);

        delete from @Replacements where tag = @CurrentTag;
    end

    -- Remove any empty tags in the format {TAG}. As SQL has no simple pattern matchijng for this, we remove anything between { and }
    declare @EmptyTagPattern varchar(255) = '{%}'
    
    while patindex('%' + @EmptyTagPattern + '%', @FinalResult) > 0
    begin
        declare @EmptyTag varchar(255);
        set @EmptyTag = substring(@FinalResult, patindex('%' + @EmptyTagPattern + '%', @FinalResult), 
                                  charindex('}', @FinalResult + '}', patindex('%' + @EmptyTagPattern + '%', @FinalResult)) 
                                  - patindex('%' + @EmptyTagPattern + '%', @FinalResult) + 1);

        -- Ensure the tag is not in the replacements table
        if not exists (select 1 from @Replacements where tag = @EmptyTag)
        begin
            set @FinalResult = replace(@FinalResult, @EmptyTag, '');
        end
    end
    return @FinalResult
end