# Class: MicroM.Data.EntityData

## Overview
Provides high level CRUD operations for an entity definition using an `IEntityClient`.

## Methods
| Method | Description |
|:--|:--|
| InsertData(ct, throw_dbstat_exception) | Executes the entity's insert stored procedure. |
| UpdateData(ct, throw_dbstat_exception) | Executes the entity's update stored procedure. |
| DeleteData(ct, throw_dbstat_exception) | Executes the entity's drop stored procedure. |
| GetData(ct) / GetData<T>(...) | Retrieves a record into the entity or typed model. |
| LookupData(ct, lookup_name) | Performs a lookup using configured views. |
| ExecuteProc*, ExecuteView | Helper methods for stored procedures and views. |

## Properties
| Property | Type | Description |
|:--|:--|:--|
| EntityClient | IEntityClient | Data access client used for operations. |
| Encryptor | IMicroMEncryption? | Optional encryption service for column values. |

## Remarks
Central component for interacting with database-stored entities.
