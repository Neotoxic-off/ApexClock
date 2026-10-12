namespace Apex_Clock.Formatting;

/// <summary>Human-friendly durations.</summary>
public static class DurationText
{
    /// <summary>"3 hours, 55 mins", "55 mins", "40 secs".</summary>
    public static string Format(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            duration = TimeSpan.Zero;

        int hours = (int)duration.TotalHours;
        int minutes = duration.Minutes;
        if (hours > 0)
            return minutes > 0 ? $"{Plural(hours, "hour")}, {Plural(minutes, "min")}" : Plural(hours, "hour");
        if (minutes > 0)
            return Plural(minutes, "min");
        return Plural(duration.Seconds, "sec");
    }

    /// <summary>Clock-style countdown, e.g. "03:55:12".</summary>
    public static string Countdown(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            duration = TimeSpan.Zero;
        return $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
    }

    /// <summary>Compact length of a slot, e.g. "4h30" or "24h".</summary>
    public static string Compact(int minutes) =>
        minutes % 60 == 0 ? $"{minutes / 60}h" : $"{minutes / 60}h{minutes % 60:00}";

    private static string Plural(int value, string unit) => value == 1 ? $"{value} {unit}" : $"{value} {unit}s";
}
