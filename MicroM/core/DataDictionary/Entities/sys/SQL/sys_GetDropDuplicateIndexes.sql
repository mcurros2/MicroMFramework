create or alter proc sys_GetDropDuplicateIndexes as

select	'table_name'=object_name(a.object_id)
		, a.name
		, a.type_desc
		, a.is_primary_key
		, a.is_unique
		, a.is_unique_constraint
		, a.object_id
		, 'columnas'=string_agg(c.name, ', ')
into	#indices
from	sys.indexes a
		JOIN sys.index_columns b ON a.object_id = b.object_id AND a.index_id=b.index_id
		JOIN sys.columns c ON b.object_id = c.object_id AND b.column_id = c.column_id
group by a.object_id, a.name, a.type_desc, a.is_primary_key, a.is_unique, a.is_unique_constraint

select	*
from
(
select	'sql'='drop index ['+a.table_name+'].['+a.name+']'
		, c.type_desc
		, rownum = ROW_NUMBER() over (partition by a.table_name order by a.table_name, a.is_primary_key desc, a.is_unique_constraint desc, a.is_unique desc)
from	#indices a
		join #indices b
		on(b.table_name=a.table_name and b.name<>a.name and b.columnas=a.columnas)
		join sys.objects c
		on(c.object_id=a.object_id and c.type='U')
) x
where x.rownum > 1

drop table #indices
