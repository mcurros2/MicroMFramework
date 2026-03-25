create or alter proc sys_GetTimeZoneOffset as

select datediff(hour, getutcdate(), getdate()) AS OffsetHoursFromUTC