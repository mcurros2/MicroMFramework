if type_id(N'EmailTagsTableType') is null
begin

CREATE TYPE EmailTagsTableType 
   AS TABLE
      ( tag VARCHAR(255)
      , value varchar(max))

end