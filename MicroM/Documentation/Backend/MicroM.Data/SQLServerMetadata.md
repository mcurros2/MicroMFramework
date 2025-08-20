# Class: MicroM.Data.SQLServerMetadata

## Overview
Captures SQL Server type information such as size, precision, scale, and nullability.

## Properties
| Property | Type | Description |
|:--|:--|:--|
| SQLType | SqlDbType | Database type of the column. |
| Size | int | Length for character or binary types. |
| Precision | byte | Numeric precision. |
| Scale | byte | Numeric scale. |
| Output | bool | Indicates if column is an output parameter. |
| Nullable | bool | Whether the column allows nulls. |
| Encrypted | bool | Whether values are encrypted. |
| IsArray | bool | Marks the column as an array type. |
