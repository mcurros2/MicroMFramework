create or alter proc [dbo].mty_InsertChunk @json_entities varchar(max), @webusr varchar(255) as

declare @now datetime = getdate(), @login sysname = original_login()

insert  [dbo].microm_entities_types
select  vc_entity_name, vc_entity_type, @now, @now, @webusr, @webusr, @login, @login
from    openjson(@json_entities)
        with (
            vc_entity_name varchar(255) '$.vc_entity_name',
            vc_entity_type varchar(255) '$.vc_entity_type'
        )

select 0, 'OK'
