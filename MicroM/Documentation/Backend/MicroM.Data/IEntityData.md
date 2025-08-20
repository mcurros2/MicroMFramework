# Interface: MicroM.Data.IEntityData

## Overview
Contract implemented by classes that perform CRUD operations against the database for a specific entity.

## Methods
| Method | Description |
|:--|:--|
| InsertData(ct, throw_dbstat_exception) | Inserts a record. |
| UpdateData(ct, throw_dbstat_exception) | Updates a record. |
| DeleteData(ct, throw_dbstat_exception) | Deletes a record. |
| ExecuteProc*(...) / ExecuteView(...) | Executes stored procedures or views. |
| LookupData(ct, lookup_name) | Performs lookups for related data. |
| GetData(ct) / GetData<T>(...) | Retrieves entity data into memory. |
