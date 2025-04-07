create or alter function emq_fGetTags(@tags EmailTagsTableType READONLY) returns nvarchar(max) as
begin
return
(
    select	tag,
            value
    from    (select * from @tags) as Tags(tag, value)
    for json path
)
end
