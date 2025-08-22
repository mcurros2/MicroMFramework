# Class: MicroM.Core.EntityDefinition
## Overview
Defines the structure and metadata of an entity.

**Inheritance**
object -> EntityDefinition

**Implements**
None

## Example Usage
```csharp
class MyDefinition : EntityDefinition {
    public MyDefinition() : base("MN", nameof(MyEntity)) {}
}
```
## Constructors
| Constructor | Description |
|:------------|:-------------|
| EntityDefinition(string mneo, string name, bool add_default_columns, bool webusr_delete_flag) | Base constructor for definitions. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Mneo | string | Mnemonic code for procedures. |
| Name | string | Class name. |
| TableName | string | Table name in SQL Server. |
| Fake | bool | Indicates a fake entity. |
| SQLCreationOptions | SQLCreationOptionsMetadata | SQL generation options. |
| Columns | IReadonlyOrderedDictionary<ColumnBase> | Columns for the entity. |
| KeyColumnName | string | Primary key column name. |
| Views | IReadOnlyDictionary<string, ViewDefinition> | Defined views. |
| Procs | IReadOnlyDictionary<string, ProcedureDefinition> | Defined procedures. |
| ForeignKeys | IReadOnlyDictionary<string, EntityForeignKeyBase> | Foreign keys. |
| Actions | IReadOnlyDictionary<string, EntityActionBase> | Entity actions. |
| UniqueConstraints | IReadOnlyDictionary<string, EntityUniqueConstraint> | Unique constraints. |
| Indexes | IReadOnlyDictionary<string, EntityIndex> | Indexes. |
| AutonumColumn | ColumnBase | Auto-number column. |
| dt_inserttime | Column<DateTime> | Creation timestamp. |
| dt_lu | Column<DateTime> | Last update timestamp. |
| vc_webinsuser | Column<string> | Creating web user. |
| vc_webluuser | Column<string> | Updating web user. |
| vc_insuser | Column<string> | Creating AD user. |
| vc_luuser | Column<string> | Updating AD user. |
| webusr | Column<string> | Creating web user name. |
| RelatedCategories | IReadOnlySet<string> | Related category IDs. |
| RelatedStatus | IReadOnlySet<string> | Related status IDs. |

## Methods
| Method | Description |
|:------------|:-------------|
| GetForeignKey<T>(T parent_entity) | Gets foreign key relating to a parent entity. |
| AddCategoryID(string category_id) | Relates a category with the entity. |
| AddStatusID(string status_id) | Relates a status with the entity. |
| DefineActions() | Adds custom actions (override). |
| AddCategoriesRelations() | Adds category relations (override). |
| AddStatusRelations() | Adds status relations (override). |
| DefineViews() | Adds view definitions (override). |
| DefineProcs() | Adds procedure definitions (override). |
| DefineConstraints() | Adds foreign key and constraint definitions (override). |

## Remarks
None.

