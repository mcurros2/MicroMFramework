create or alter function emq_fGetEmailDestinationAndTags(
	@reference_id varchar(255)
	, @destination_email varchar(2048)
	, @destination_name varchar(255)
	, @tags EmailTagsTableType READONLY
	) returns varchar(max) as
begin
return
	(
	select	reference_id = @reference_id,
			destination_email = trim(@destination_email),
			destination_name = trim(@destination_name),
			(
				select	tag,
						value
				from    (
							select tag, value from @tags
						) as Tags(tag, value)
				for json path
			) as tags
			for json path
	)
end