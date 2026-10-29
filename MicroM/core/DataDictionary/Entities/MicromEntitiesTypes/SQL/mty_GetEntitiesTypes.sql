create or alter proc [dbo].mty_GetEntitiesTypes as

select  vc_entity_name, vc_entity_type
from    [dbo].microm_entities_types
