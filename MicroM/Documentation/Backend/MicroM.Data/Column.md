# Class: MicroM.Data.Column<T>

## Overview
Typed column that carries value and SQL metadata for use in queries and parameters.

## Methods
| Method | Description |
|:--|:--|
| PK(...) | Factory creating a primary key column with appropriate flags. |
| FK(...) | Factory creating a foreign key column. |
| Text(...) | Factory for variable-length text columns. |
| Char(...) | Factory for fixed-length text columns. |
| Clone() | Creates a copy of the column. |

## Properties
| Property | Type | Description |
|:--|:--|:--|
| Value | T | Strongly typed column value. |

## Remarks
Extends `ColumnBase` to provide generic typed access and helper factories for common column types.
