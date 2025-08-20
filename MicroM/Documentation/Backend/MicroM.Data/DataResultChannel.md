# Class: MicroM.Data.DataResultChannel

## Overview
Channel-based stream of result rows supporting asynchronous consumption.

## Properties
| Property | Type | Description |
|:--|:--|:--|
| Header | string[] | Column names for the streamed data. |
| Records | Channel<object[]> | Bounded channel holding rows. |
