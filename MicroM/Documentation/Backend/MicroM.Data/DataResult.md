# Class: MicroM.Data.DataResult

## Overview
In-memory representation of tabular data returned from a query.

## Properties
| Property | Type | Description |
|:--|:--|:--|
| Header | string[] | Column names for the result. |
| typeInfo | string[] | Type names for each column. |
| records | List<object?[]> | Rows returned from the database. |

## Remarks
Generic variant `DataResult<T>` stores typed records.
