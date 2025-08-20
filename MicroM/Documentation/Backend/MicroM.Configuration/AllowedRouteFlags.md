# Enum: MicroM.Configuration.AllowedRouteFlags

## Overview
Flags representing permitted operations for a route within the security subsystem.

## Values
| Value | Description |
|:--|:--|
| None | No operations are allowed. |
| Insert | Grants insert operations. |
| Update | Grants update operations. |
| Delete | Grants delete operations. |
| Get | Allows retrieval operations. |
| DefaultLookup | Permits default lookup routes. |
| Edit | Combination of insert, update, delete, and get. |
| CustomLookup | Permits custom lookup logic. |
| Views | Allows access to view routes. |
| Procs | Allows execution of procedures. |
| Actions | Grants access to action routes. |
| Import | Enables import routes. |
| All | Enables all basic operations. |
| AllWithImport | Enables all operations including import. |

## Remarks
Use these flags to configure which endpoints remain accessible without authentication.

## See Also
- [SecurityDefaults](SecurityDefaults.md)
- [Backend Namespaces](../index.md)
