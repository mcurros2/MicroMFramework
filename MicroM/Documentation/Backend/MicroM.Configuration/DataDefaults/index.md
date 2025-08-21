# Class: MicroM.Configuration.DataDefaults
## Overview
Default values for data access operations.

**Inheritance**
object -> DataDefaults

**Implements**
None

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| DefaultConnectionTimeOutInSecs | int | Connection timeout in seconds. |
| DefaultCommandTimeOutInMins | int | Query time out in minutes. |
| DateFormat | string | Format to convert date time to SQL string. |
| DefaultChannelRecordsBuffer | int | Max items for the channel that holds records for a result. |
| DefaultChannelResultsBuffer | int | Max items for the channel that holds DataResultChannel in a DataResultSetChannel. |
| AppendDBOtoProcs | bool | Indicates to append "dbo." to a stored procedure if owner is not specified. |
| DefaultRowLimitForViews | int | The default limit for returned rows from a view. |
| RowLimitParameterName | string | Parameter name for row_limit when executing views. |

## Remarks
None.

## See Also
-
