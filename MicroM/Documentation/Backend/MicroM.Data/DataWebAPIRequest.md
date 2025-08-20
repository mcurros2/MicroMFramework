# Class: MicroM.Data.DataWebAPIRequest

## Overview
Holds values submitted to data APIs, including parent keys, record selections, and server claims.

## Properties
| Property | Type | Description |
|:--|:--|:--|
| ParentKeys | Dictionary<string, object>? | Keys from parent entities. |
| Values | Dictionary<string, object> | Column values for the request. |
| RecordsSelection | List<Dictionary<string, object>> | Rows selected on the client. |
| ServerClaims | Dictionary<string, object>? | Claims supplied by the server. |
