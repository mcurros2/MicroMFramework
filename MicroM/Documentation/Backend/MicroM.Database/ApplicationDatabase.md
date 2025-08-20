# Class: MicroM.Database.ApplicationDatabase

## Overview
Manages creation, status checks, updates, and deletion for application-specific databases.

## Methods
| Method | Description |
|:--|:--|
| GetAppDatabaseStatus | Retrieves status flags for server connectivity, admin rights, and database/user existence. |
| DropAppDatabase | Drops the application database and recreates it along with required logins. |
| UpdateAppDatabase | Applies migrations and ensures users and schema are up to date. |

## Remarks
Used by the MicroM control panel to provision and maintain tenant databases.

## See Also
- [DatabaseManagement](DatabaseManagement.md)

