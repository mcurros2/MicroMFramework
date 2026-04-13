create or alter proc [dbo].sys_GetTimeZoneOffset as

select datediff(hour, getutcdate(), getdate()) AS OffsetHoursFromUTC