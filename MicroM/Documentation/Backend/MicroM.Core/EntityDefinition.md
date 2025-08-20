# Class: MicroM.Core.EntityDefinition

## Overview
Base class that describes the structure of an entity including columns, procedures, views, constraints, and actions.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| EntityDefinition(string mneo, string name, bool add_default_columns = true, bool webusr_delete_flag = false) | Initializes the definition with a mnemonic code and table name and optionally adds default columns. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Mneo | string | Mnemonic code used as prefix for stored procedures. |
| Name | string | Name of the entity class. |
| TableName | string | Table name in the database. |
| Fake | bool | Indicates whether the entity represents a table. |
| Columns | IReadonlyOrderedDictionary<ColumnBase> | Columns defined for the entity. |
| Views | IReadOnlyDictionary<string, ViewDefinition> | Views associated with the entity. |
| Procs | IReadOnlyDictionary<string, ProcedureDefinition> | Procedures associated with the entity. |
| ForeignKeys | IReadOnlyDictionary<string, EntityForeignKeyBase> | Foreign key definitions. |
| Actions | IReadOnlyDictionary<string, EntityActionBase> | Custom actions defined for the entity. |
| UniqueConstraints | IReadOnlyDictionary<string, EntityUniqueConstraint> | Unique constraints defined for the entity. |
| Indexes | IReadOnlyDictionary<string, EntityIndex> | Index definitions. |

## Methods
| Method | Description |
|:------------|:-------------|
| GetForeignKey<T>(T parent_entity) | Retrieves the foreign key relationship for a parent entity. |
| AddStatusID(string status_id) | Registers a related status identifier. |

## Remarks
Derived classes override `DefineProcs`, `DefineViews`, `DefineConstraints`, `DefineActions`, and related methods to provide entity-specific metadata. Validation ensures procedure names start with the entity mnemonic.

## See Also
- [Entity](Entity.md)
- [EntityBase](EntityBase.md)
