# Class: MicroM.Data.ViewDefinition

## Overview
Maps a stored procedure that behaves as a view, holding its parameters and optional restrictions.

## Properties
| Property | Type | Description |
|:--|:--|:--|
| Parms | Dictionary<string, ViewParm> | View parameters keyed by name. |

## Remarks
Used by `Entity` classes to execute browse-style stored procedures.
