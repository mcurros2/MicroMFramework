# Class: MicroM.Configuration.DataDefaults

## Overview
Default values for data access operations.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| *(none)* | Not instantiable.

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| DefaultConnectionTimeOutInSecs | int | Connection timeout in seconds. |
| DefaultCommandTimeOutInMins | int | Query time out in minutes. |
| DateFormat | string | Format to convert date time to SQL string. |
| DefaultChannelRecordsBuffer | int | Max items for channel holding records for a result. |
| DefaultChannelResultsBuffer | int | Max items for channel holding result sets. |
| AppendDBOtoProcs | bool | Indicates to append "dbo." to a stored procedure if owner is not specified. |
| DefaultRowLimitForViews | int | Default limit for returned rows from a view. |
| RowLimitParameterName | string | Parameter name for row_limit in views. |

## Methods
| Method | Description |
|:------------|:-------------|
| *(none)* | |

## Remarks
These defaults influence data access behavior across MicroM.

## See Also
- [ConfigurationDefaults](ConfigurationDefaults.md)
