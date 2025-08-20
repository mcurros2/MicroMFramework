# Namespace: MicroM.Data

## Overview
Provides data access abstractions, column metadata, and SQL helper utilities for the MicroM backend.

## Classes
| Class | Description |
|:--|:--|
| [BaseColumnMapping](BaseColumnMapping.md) | Maps parent and child columns for foreign keys. |
| [Column](Column.md) | Generic column with SQL metadata and helper factories. |
| [ColumnBase](ColumnBase.md) | Abstract base for all column types. |
| [CompoundColumnItem](CompoundColumnItem.md) | Represents a column within a compound key group. |
| [DBStatus](DBStatus.md) | Database status information with optional message. |
| [DBStatusResult](DBStatusResult.md) | Aggregates multiple database status responses. |
| [DataAbstractionException](DataAbstractionException.md) | Error raised during data access operations. |
| [DataResult](DataResult.md) | Holds tabular data returned from queries. |
| [DataResultChannel](DataResultChannel.md) | Streams result rows over channels. |
| [DataResultSetChannel](DataResultSetChannel.md) | Streams multiple result sets. |
| [DataWebAPIRequest](DataWebAPIRequest.md) | Values submitted to data APIs. |
| [DatabaseClient](DatabaseClient.md) | SQL Server client implementing entity operations. |
| [DefaultColumns](DefaultColumns.md) | Predefined system columns used for auditing. |
| [EntityData](EntityData.md) | High level CRUD operations for an entity. |
| [EntityFilter](EntityFilter.md) | Filter binding an entity to a filter entity type. |
| [EntityFilterBase](EntityFilterBase.md) | Base class for entity filters. |
| [EntityForeignKey](EntityForeignKey.md) | Generic foreign key linking parent and child entities. |
| [EntityForeignKeyBase](EntityForeignKeyBase.md) | Base relationship definition with lookups. |
| [EntityIndex](EntityIndex.md) | Defines an index over entity columns. |
| [EntityLookup](EntityLookup.md) | Lookup definition for related entities. |
| [EntityUniqueConstraint](EntityUniqueConstraint.md) | Uniqueness constraint across columns. |
| [ProcedureDefinition](ProcedureDefinition.md) | Stored procedure description and parameters. |
| [SqlDbTypeMapper](SqlDbTypeMapper.md) | Maps `SqlDbType` to .NET types. |
| [SQLServerMetadata](SQLServerMetadata.md) | SQL type and size metadata for columns. |
| [SystemColumnNames](SystemColumnNames.md) | Constant names for audit columns. |
| [SystemStandardProceduresSuffixs](SystemStandardProceduresSuffixs.md) | Suffixes for generated stored procedures. |
| [SystemViewParmNames](SystemViewParmNames.md) | Standard view parameter names. |
| [ValueReader](ValueReader.md) | Implementation of `IGetFieldValue` for `SqlDataReader`. |
| [ViewDefinition](ViewDefinition.md) | View stored procedure mapping. |
| [ViewParm](ViewParm.md) | Parameter definition for views. |

## Enums
| Enum | Description |
|:--|:--|
| [ColumnFlags](ColumnFlags.md) | Flags describing column behavior in CRUD operations. |
| [DBStatusCodes](DBStatusCodes.md) | Database procedure outcome codes. |
| [SQLCreationOptionsMetadata](SQLCreationOptionsMetadata.md) | Options for generating companion procedures. |

## Interfaces
| Interface | Description |
|:--|:--|
| [IGetFieldValue](IGetFieldValue.md) | Accessor for typed values from data readers. |
| [IEntityClient](IEntityClient.md) | Database client for executing commands and managing connections. |
| [IEntityData](IEntityData.md) | CRUD operations contract for entity data. |

## Remarks
Data namespace components supply the foundation for persisting and retrieving entity information from SQL Server.

## See Also
- [Backend Namespaces](../index.md)
