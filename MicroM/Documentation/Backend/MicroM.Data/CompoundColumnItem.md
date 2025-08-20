# Class: MicroM.Data.CompoundColumnItem

## Overview
Represents a column within a compound key definition, tracking position and whether it participates in the key.

## Properties
| Property | Type | Description |
|:--|:--|:--|
| Column | ColumnBase | Column metadata. |
| Position | int | Position of the column in the compound group. |
| CompoundKey | bool | Indicates if the column is part of the compound key. |
