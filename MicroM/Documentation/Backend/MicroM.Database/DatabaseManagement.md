# Class: MicroM.Database.DatabaseManagement

## Overview
Provides low-level SQL Server management utilities such as creating databases and logins.

## Methods
| Method | Description |
|:--|:--|
| LoggedInUserHasAdminRights | Checks if the current SQL connection has admin rights. |
| UserExists | Determines whether a SQL login exists. |
| DatabaseExists | Determines whether a database exists. |
| ServerIsUp | Tests connectivity to the SQL server. |
| CreateDatabase | Creates a database with optional collation and simple recovery. |

## Remarks
Utility methods used by higher-level database provisioning routines.

