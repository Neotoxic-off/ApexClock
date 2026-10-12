namespace ApexClock.Core;

/// <summary>A wall-clock time of day with minute precision, e.g. 19:30.</summary>
public readonly record struct ClockTime : IComparable<ClockTime>
{
    public const int MinutesPerDay = 24 * 60;

    public ClockTime(int minuteOfDay)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minuteOfDay);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minuteOfDay, MinutesPerDay);
        MinuteOfDay = minuteOfDay;
    }

    /// <summary>Minutes after midnight, 0 to 1439.</summary>
    public int MinuteOfDay { get; }

    public int Hour => MinuteOfDay / 60;

    public int Minute => MinuteOfDay % 60;

    /// <summary>Time of day of <paramref name="instant"/> in <paramref name="zone"/>.</summary>
    public static ClockTime At(DateTimeOffset instant, TimeZoneInfo zone)
    {
        DateTime local = TimeZoneInfo.ConvertTime(instant, zone).DateTime;
        return new ClockTime(local.Hour * 60 + local.Minute);
    }

    /// <summary>This time shifted by <paramref name="minutes"/>, wrapping around midnight.</summary>
    public ClockTime AddMinutes(int minutes) =>
        new ClockTime(((MinuteOfDay + minutes) % MinutesPerDay + MinutesPerDay) % MinutesPerDay);

    /// <summary>Minutes from this time to the next occurrence of <paramref name="later"/>: 1 to 1440, a full day when equal.</summary>
    public int MinutesUntil(ClockTime later)
    {
        int difference = later.MinuteOfDay - MinuteOfDay;
        return difference > 0 ? difference : difference + MinutesPerDay;
    }

    /// <summary>Accepts "19", "19h", "19h30", "19:30", "19.30" and "1930".</summary>
    public static bool TryParse(string? text, out ClockTime time)
    {
        time = default;
        string value = (text ?? "").Trim().ToLowerInvariant().Replace('h', ':').Replace('.', ':');
        if (value.Length == 0)
            return false;

        string[] parts = value.Split(':');
        string hourText;
        string minuteText;
        if (parts.Length == 1 && value.Length > 2)
        {
            hourText = value[..^2];
            minuteText = value[^2..];
        }
        else if (parts.Length is 1 or 2)
        {
            hourText = parts[0];
            minuteText = parts.Length == 2 ? parts[1] : "";
        }
        else
        {
            return false;
        }

        if (!int.TryParse(hourText, out int hour) || hour is < 0 or > 23)
            return false;
        int minute = 0;
        if (minuteText.Length > 0 && (!int.TryParse(minuteText, out minute) || minute is < 0 or > 59))
            return false;

        time = new ClockTime(hour * 60 + minute);
        return true;
    }

    public int CompareTo(ClockTime other) => MinuteOfDay.CompareTo(other.MinuteOfDay);

    public override string ToString() => $"{Hour:00}:{Minute:00}";
}
