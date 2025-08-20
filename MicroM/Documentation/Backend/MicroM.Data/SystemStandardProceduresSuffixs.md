# Class: MicroM.Data.SystemStandardProceduresSuffixs

## Overview
Provides standard suffix constants for generated stored procedure names and a helper to infer procedure type.

## Fields
| Field | Description |
|:--|:--|
| _update | Update procedure suffix. |
| _drop | Drop procedure suffix. |
| _get | Get procedure suffix. |
| _brwStandard | Standard browse view suffix. |
| _lookup | Lookup procedure suffix. |
| _iupdate | Internal update suffix. |
| _idrop | Internal drop suffix. |

## Methods
| Method | Description |
|:--|:--|
| GetProcStandardType(proc_name) | Returns a `SQLProcStandardType` based on suffix. |
