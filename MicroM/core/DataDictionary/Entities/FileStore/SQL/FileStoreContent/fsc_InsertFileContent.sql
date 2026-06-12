create or alter proc [dbo].fsc_InsertFileContent	@file_id char(20),	@file_content varbinary(max), @webusr varchar(255) as

-- This SP is designed for streaming from the backend
declare @now datetime = getdate(), @login sysname=original_login()

insert	[dbo].file_store_content
values	(@file_id, @file_content, @now, @now, @webusr, @webusr, @login, @login)
