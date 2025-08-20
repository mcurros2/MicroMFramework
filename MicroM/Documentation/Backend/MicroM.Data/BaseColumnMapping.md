# Class: MicroM.Data.BaseColumnMapping

## Overview
Maps a parent column name to a child column name for foreign key relationships.

## Properties
| Property | Type | Description |
|:--|:--|:--|
| ParentColName | string | Name of the column in the parent entity. |
| ChildColName | string | Name of the column in the child entity. |

## Remarks
Used by `EntityForeignKey` to relate columns between entities.
