if type_id(N'[dbo].EmailTagsTableType') is null
begin

CREATE TYPE [dbo].EmailTagsTableType 
   AS TABLE
      ( tag VARCHAR(255)
      , value varchar(max))

end