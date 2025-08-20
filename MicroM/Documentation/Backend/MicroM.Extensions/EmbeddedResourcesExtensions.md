# Static Class: MicroM.Extensions.EmbeddedResourcesExtensions

## Overview
Retrieves embedded SQL scripts or other resources from an assembly.

## Methods
| Method | Description |
|:--|:--|
| GetAllCustomProcs<T> | Reads all embedded SQL files optionally filtered by prefix. |
| GetAssemblyCustomProcs | Reads embedded SQL files from a given assembly with filters. |

## Remarks
These helpers support database schema generation by loading SQL templates packaged as resources.
