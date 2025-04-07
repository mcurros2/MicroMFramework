namespace MicroM.Extensions
{
    public static class TimeExtensions
    {
        public static string ToHumanDuration(this TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
            {
                // Format as days, hours, minutes, seconds
                return $"{duration.Days} day(s), {duration.Hours} hour(s), {duration.Minutes} minute(s), {duration.Seconds} second(s)";
            }
            else if (duration.TotalHours >= 1)
            {
                // Format as hours, minutes, seconds
                return $"{duration.Hours} hour(s), {duration.Minutes} minute(s), {duration.Seconds} second(s)";
            }
            else if (duration.TotalMinutes >= 1)
            {
                // Format as minutes, seconds
                return $"{duration.Minutes} minute(s), {duration.Seconds} second(s)";
            }
            else if (duration.TotalSeconds >= 1)
            {
                // Format as seconds
                return $"{duration.Seconds} second(s), {duration.Milliseconds} millisecond(s)";
            }
            else
            {
                // Format as milliseconds
                return $"{duration.TotalMilliseconds} millisecond(s)";
            }
        }

    }
}
