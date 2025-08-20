# Class: MicroM.Core.Entity<TDefinition>

## Overview
Generic base class for entities that ties an `EntityDefinition` to data access operations.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| Entity() | Creates an entity using the default definition and initializes it. |
| Entity(string table_name) | Creates an entity with a specific table name. |
| Entity(IEntityClient ec, IMicroMEncryption? encryptor) | Creates an entity with a data client and optional encryptor. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Def | TDefinition | Strongly-typed entity definition. |

## Remarks
Provides typed access to the underlying `EntityDefinition` and initialization helpers.

## See Also
- [EntityBase](EntityBase.md)
- [EntityDefinition](EntityDefinition.md)
