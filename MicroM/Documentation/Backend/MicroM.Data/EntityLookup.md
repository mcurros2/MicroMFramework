# Class: MicroM.Data.EntityLookup

## Overview
Describes how to obtain descriptive information for a parent record via a view and lookup procedure.

## Properties
| Property | Type | Description |
|:--|:--|:--|
| ViewName | string | View used for browsing parent records. |
| LookupProcName | string | Procedure to retrieve the description. |
| KeyParameter | string? | Parameter representing the key column. |
| CompoundKeyGroup | string | Compound key group used for the lookup. |
| DescriptionColumnIndex | int | Column index used as description. |
| IDColumnIndex | int | Column index used as identifier. |
