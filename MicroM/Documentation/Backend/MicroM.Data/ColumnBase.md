# Class: MicroM.Data.ColumnBase

## Overview
Abstract representation of a database column including SQL metadata and value handling.

## Properties
| Property | Type | Description |
|:--|:--|:--|
| Name | string | Column name used in database operations. |
| SQLMetadata | SQLServerMetadata | SQL type, size, and nullability information. |
| ColumnMetadata | ColumnFlags | Flags describing column behavior. |
| ValueObject | object? | Raw value assigned to the column. |

## Remarks
Serves as the base for typed `Column<T>` implementations.
