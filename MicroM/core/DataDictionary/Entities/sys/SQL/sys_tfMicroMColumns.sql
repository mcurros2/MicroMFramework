create or alter function dbo.sys_tfMicroMColumns() returns table as
return
(
select	a.object_id
		, object_name=a.name
		, column_name=b.name
		, b.column_id
		, [type]=c.name
		, b.max_length
		, b.precision
		, b.scale
		, is_primary_key=case when e.column_id is not null then d.is_primary_key else 0 end
		, b.is_nullable
		, microm_type=
			CASE 
				WHEN c.name='bigint' THEN iif(b.is_nullable=1, 'long?', 'long')
				WHEN c.name in('binary','image','timestamp','varbinary') THEN iif(b.is_nullable=1, 'byte[]?', 'byte[]')
				WHEN c.name='bit' THEN 'bool'
				WHEN c.name in('char','nchar','ntext','text','nvarchar','varchar','xml') THEN iif(b.is_nullable=1, 'string?', 'string')
				WHEN c.name in('datetime','smalldatetime','datetime2') THEN iif(b.is_nullable=1, 'DateTime?', 'DateTime')
				WHEN c.name='date' THEN iif(b.is_nullable=1, 'DateOnly?', 'DateOnly')
				WHEN c.name='time' THEN iif(b.is_nullable=1, 'TimeOnly?', 'TimeOnly')
				WHEN c.name in('decimal','money','smallmoney','numeric') THEN iif(b.is_nullable=1, 'decimal?', 'decimal')
				WHEN c.name='float' THEN iif(b.is_nullable=1, 'double?', 'double')
				WHEN c.name='int' THEN iif(b.is_nullable=1, 'int?', 'int')
				WHEN c.name='real' THEN iif(b.is_nullable=1, 'float?', 'float')
				WHEN c.name='uniqueidentifier' THEN iif(b.is_nullable=1, 'Guid?', 'Guid')
				WHEN c.name='smallint' THEN iif(b.is_nullable=1, 'short?', 'short')
				WHEN c.name='tinyint' THEN iif(b.is_nullable=1, 'byte?', 'byte')
				WHEN c.name in('variant','udt') THEN iif(b.is_nullable=1, 'object?', 'object')
				WHEN c.name='structured' THEN iif(b.is_nullable=1, 'DataTable?', 'DataTable')
				WHEN c.name='datetimeoffset' THEN iif(b.is_nullable=1, 'DateTimeOffset?', 'DateTimeOffset')
				ELSE iif(b.is_nullable=1, 'object?', 'object')
			END
from	sys.all_objects a
		join sys.all_columns b
		on(b.object_id=a.object_id)
		join sys.types c
		on(c.system_type_id=b.system_type_id)
		left join sys.indexes d
		on(d.object_id=a.object_id and d.is_primary_key=1)
		left join sys.index_columns e
		on(e.object_id=d.object_id and e.index_id=d.index_id and e.column_id=b.column_id)

)