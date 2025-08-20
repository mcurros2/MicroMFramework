# Class: MicroM.Data.DBStatusResult

## Overview
Aggregates a list of [DBStatus](DBStatus.md) responses along with flags indicating overall success and whether an autonumber was returned.

## Properties
| Property | Type | Description |
|:--|:--|:--|
| Failed | bool | Indicates if any status reported an error. |
| AutonumReturned | bool | True when an autonumber value was produced. |
| Results | List<DBStatus>? | Detailed status objects. |
