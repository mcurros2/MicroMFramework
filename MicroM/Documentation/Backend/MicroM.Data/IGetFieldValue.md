# Interface: MicroM.Data.IGetFieldValue

## Overview
Abstraction over data readers that exposes synchronous and asynchronous methods for retrieving typed column values.

## Methods
| Method | Description |
|:--|:--|
| GetFieldValue<T>(int position) | Gets a value by ordinal. |
| GetFieldValue<T>(string column_name) | Gets a value by column name. |
| GetFieldValueAsync<T>(int position, ct) | Asynchronously gets a value by ordinal. |
| GetFieldValueAsync<T>(string column_name, ct) | Asynchronously gets a value by column name. |
