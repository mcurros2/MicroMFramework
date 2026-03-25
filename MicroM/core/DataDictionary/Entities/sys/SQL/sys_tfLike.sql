create or alter function sys_tflike(@like varchar(max))
returns @result table (phrase varchar(max))
as
begin
    if isjson(@like) = 1
    begin
        insert into @result
        select phrase
        from openjson(@like) with (phrase varchar(max) '$');
    end
    else if @like is not null and @like <> ''
    begin
        insert into @result
        select @like;
    end

    return;
end
