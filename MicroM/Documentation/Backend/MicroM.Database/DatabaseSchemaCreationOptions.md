# Record: MicroM.Database.DatabaseSchemaCreationOptions<T>

## Overview
Holds an entity instance and flags controlling schema creation.

## Properties
| Property | Type | Description |
|:--|:--|:--|
| EntityInstance | T | Entity instance used for schema generation. |
| EntityType | Type | Runtime type of the entity. |
| Mneo | string | Mnemonic identifier for the entity. |
| create_or_alter | bool | Indicates whether to create new objects or alter existing ones. |

