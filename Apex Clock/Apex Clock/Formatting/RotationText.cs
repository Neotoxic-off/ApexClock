using ApexClock.Core;

namespace Apex_Clock.Formatting;

/// <summary>Texts describing a rotation slot, in local time.</summary>
public static class RotationText
{
    /// <summary>"From 10:00 to 14:30".</summary>
    public static string TimeRange(RotationState slot) =>
        $"From {slot.StartsAt.ToLocalTime():HH:mm} to {slot.EndsAt.ToLocalTime():HH:mm}";

    /// <summary>"10:00 – 14:30".</summary>
    public static string ShortTimeRange(RotationState slot) =>
        $"{slot.StartsAt.ToLocalTime():HH:mm} – {slot.EndsAt.ToLocalTime():HH:mm}";

    /// <summary>"ends in 3 hours, 55 mins".</summary>
    public static string Remaining(RotationState slot, DateTimeOffset now) =>
        $"ends in {DurationText.Format(slot.EndsAt - now)}";

    /// <summary>Elapsed share of the slot, 0 to 1.</summary>
    public static double Progress(RotationState slot, DateTimeOffset now)
    {
        TimeSpan length = slot.EndsAt - slot.StartsAt;
        return length > TimeSpan.Zero ? Math.Clamp((now - slot.StartsAt) / length, 0, 1) : 0;
    }
}
