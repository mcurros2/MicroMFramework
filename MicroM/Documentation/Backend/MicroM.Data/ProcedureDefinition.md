# Class: MicroM.Data.ProcedureDefinition

## Overview
Describes a stored procedure call including its name, parameters, and execution options.

## Properties
| Property | Type | Description |
|:--|:--|:--|
| Name | string | Stored procedure name. |
| Parms | Dictionary<string, ColumnBase> | Parameters for the procedure. |
| ReadonlyLocks | bool | Indicates if the procedure uses read-uncommitted locks. |
| isLookup | bool | Whether the procedure is used for lookups. |
| isImport | bool | Whether the procedure is used for import operations. |

## Methods
| Method | Description |
|:--|:--|
| AddParmsFromCols(...) | Adds parameters based on column templates. |
| AddParmFromCol<T>(...) | Creates and adds a typed parameter from a column. |
| SetParmsValues(cols) | Assigns column values to parameters. |
