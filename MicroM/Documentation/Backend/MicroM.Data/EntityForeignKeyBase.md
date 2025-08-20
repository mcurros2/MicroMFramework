# Class: MicroM.Data.EntityForeignKeyBase

## Overview
Base class describing a relationship between parent and child entities along with key column mappings and lookups.

## Properties
| Property | Type | Description |
|:--|:--|:--|
| Name | string | Name of the foreign key. |
| ParentEntityType | Type | Parent entity type. |
| ChildEntityType | Type | Child entity type. |
| KeyMappings | List<BaseColumnMapping> | Column mappings between entities. |
| EntityLookups | Dictionary<string, EntityLookup> | Named lookups for the relationship. |

## Methods
| Method | Description |
|:--|:--|
| AddLookup(view, lookup, ...) | Registers a lookup for the foreign key. |
