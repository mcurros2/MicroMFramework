# Class: MicroM.Extensions.CoreExtensions
## Overview
Extensions for entities, procedures, and views.

**Inheritance**
object -> CoreExtensions

**Implements**
None

## Example Usage
```csharp
var proc = new Dictionary<string, ProcedureDefinition>().AddProc("DoWork", false);
```
## Methods
| Method | Description |
|:------------|:-------------|
| AddProc(Dictionary<string, ProcedureDefinition> collection, string name, bool readonly_locks = false, params ColumnBase[] parms) | Adds a procedure definition to a collection. |
| AddFK<TParent, TChild>(Dictionary<string, EntityForeignKeyBase> collection, string name, bool fake = false, bool do_not_create_index = false, List<BaseColumnMapping>? key_mappings = null) where TParent : EntityBase where TChild : EntityBase | Registers a foreign key definition. |
| AddView(Dictionary<string, ViewDefinition> collection, string name, bool add_default_parms = true, IReadonlyOrderedDictionary<ColumnBase>? parms_columns = null) | Adds a view definition. |
| SetKeyValues(EntityBase entity, Dictionary<string, object> values) | Sets key column values on an entity. |
| SetColumnValues(EntityBase entity, Dictionary<string, object> values) | Assigns column values on an entity. |
| SetColumnValue(EntityBase entity, string col_name, Dictionary<string, object> values) | Sets a single column value on an entity. |
| DefineProc<T>(T def, bool readonly_locks = false, params string[] column_names) where T : EntityDefinition | Defines a procedure on an entity definition. |
| ToTableName<T>() | Returns the table name for an entity type. |
| Clone<T>(T original, bool clone_connection) where T : EntityBase, new() | Creates a copy of an entity. |
| CopyFrom(EntityBase entity, EntityBase source) | Copies values from another entity. |
| IsFileExtensionAllowed(string fileName, string[] allowedExtensions) | Checks whether a file extension is allowed. |

## Remarks
None.

## See Also
-
