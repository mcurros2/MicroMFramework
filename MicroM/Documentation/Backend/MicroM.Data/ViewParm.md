# Class: MicroM.Data.ViewParm

## Overview
Represents a parameter definition for a view, including mapping to entity columns and compound key information.

## Properties
| Property | Type | Description |
|:--|:--|:--|
| Column | ColumnBase | Column metadata for the parameter. |
| ColumnMapping | int | Index mapping to view results. |
| CompoundGroup | string | Name of the compound key group. |
| CompoundPosition | int | Position within compound group. |
| CompoundKey | bool | Indicates if parameter participates in compound key. |
| BrowsingKey | bool | Marks parameter as a browsing key. |
