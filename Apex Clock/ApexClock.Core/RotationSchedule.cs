namespace ApexClock.Core;

public sealed class RotationEntry
{
    public string Map { get; set; } = "";

    /// <summary>Local time of day the map goes live, in minutes after midnight.</summary>
    public int StartMinute { get; set; }
}

/// <summary>Position in the rotation at a given instant.</summary>
/// <param name="SlotId">Unique per (day, entry) pair; changes every time the rotation advances.</param>
public sealed record RotationState(
    int Index,
    long SlotId,
    string CurrentMap,
    string NextMap,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);

/// <summary>
/// Daily ranked map rotation: each entry goes live at its start time and lasts until the next entry's start,
/// the last one running until the first entry of the following day.
/// </summary>
public sealed class RotationSchedule
{
    public const int MaxEntries = 50;
    public const int MaxMapNameLength = 64;
    public const int MinutesPerDay = 24 * 60;

    public static readonly IReadOnlyList<string> KnownMaps =
    [
        "Kings Canyon",
        "World's Edge",
        "Olympus",
        "Storm Point",
        "Broken Moon",
        "E-District",
    ];

    /// <summary>Windows time zone the start times are expressed in.</summary>
    public string TimeZoneId { get; set; } = TimeZoneInfo.Local.Id;

    /// <summary>Entries in any order; the rotation follows their start times.</summary>
    public List<RotationEntry> Entries { get; set; } = [];

    private TimeZoneInfo? _zone;

    public static RotationSchedule CreateDefault() => new RotationSchedule
    {
        Entries =
        [
            new RotationEntry { Map = "Olympus", StartMinute = 10 * 60 },
            new RotationEntry { Map = "Broken Moon", StartMinute = 14 * 60 + 30 },
            new RotationEntry { Map = "World's Edge", StartMinute = 19 * 60 },
        ],
    };

    /// <summary>Returns an error message, or null when the schedule is usable.</summary>
    public string? Validate()
    {
        if (Entries is null || Entries.Count == 0)
            return "The rotation needs at least one map.";
        if (Entries.Count > MaxEntries)
            return $"The rotation can hold at most {MaxEntries} maps.";
        if (Zone() is null)
            return $"Unknown time zone \"{TimeZoneId}\".";

        HashSet<int> starts = new HashSet<int>();
        for (int i = 0; i < Entries.Count; i++)
        {
            RotationEntry? entry = Entries[i];
            if (entry is null || string.IsNullOrWhiteSpace(entry.Map))
                return $"Row {i + 1}: map name is empty.";
            if (entry.Map.Length > MaxMapNameLength)
                return $"Row {i + 1}: map name is too long.";
            if (entry.StartMinute is < 0 or >= MinutesPerDay)
                return $"Row {i + 1}: start time must be between 00:00 and 23:59.";
            if (!starts.Add(entry.StartMinute))
                return $"Row {i + 1}: another map already starts at {FormatTime(entry.StartMinute)}.";
        }

        return null;
    }

    /// <summary>Trimmed copy with entries sorted by start time.</summary>
    public RotationSchedule Normalized() => new RotationSchedule
    {
        TimeZoneId = string.IsNullOrWhiteSpace(TimeZoneId) ? TimeZoneInfo.Local.Id : TimeZoneId,
        Entries = (Entries ?? [])
            .Select((RotationEntry? e) => new RotationEntry { Map = e?.Map?.Trim() ?? "", StartMinute = e?.StartMinute ?? 0 })
            .OrderBy((RotationEntry e) => e.StartMinute)
            .ToList(),
    };

    public RotationState? GetState(DateTimeOffset now)
    {
        if (Validate() is not null)
            return null;

        TimeZoneInfo zone = Zone()!;
        List<RotationEntry> sorted = Entries.OrderBy((RotationEntry e) => e.StartMinute).ToList();
        DateOnly today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);

        // Walk the slots from yesterday's first to tomorrow's last; the live one is the last start at or before now.
        int current = -1;
        DateOnly currentDay = today;
        bool passed = false;
        for (DateOnly day = today.AddDays(-1); day <= today.AddDays(1) && !passed; day = day.AddDays(1))
        {
            for (int i = 0; i < sorted.Count; i++)
            {
                if (StartOf(zone, day, sorted[i].StartMinute) > now)
                {
                    passed = true;
                    break;
                }
                current = i;
                currentDay = day;
            }
        }

        int next = (current + 1) % sorted.Count;
        DateOnly nextDay = next == 0 ? currentDay.AddDays(1) : currentDay;
        return new RotationState(
            Entries.IndexOf(sorted[current]),
            (long)currentDay.DayNumber * MaxEntries + current,
            sorted[current].Map,
            sorted[next].Map,
            StartOf(zone, currentDay, sorted[current].StartMinute),
            StartOf(zone, nextDay, sorted[next].StartMinute));
    }

    /// <summary>The <paramref name="count"/> slots following the current one.</summary>
    public IReadOnlyList<RotationState> GetUpcoming(DateTimeOffset now, int count)
    {
        List<RotationState> upcoming = new List<RotationState>(count);
        RotationState? state = GetState(now);
        while (state is not null && upcoming.Count < count)
        {
            state = GetState(state.EndsAt);
            if (state is not null)
                upcoming.Add(state);
        }
        return upcoming;
    }

    public static string FormatTime(int minuteOfDay) => $"{minuteOfDay / 60:00}:{minuteOfDay % 60:00}";

    /// <summary>Parses "19", "19h", "19h30", "19:30" or "1930" into minutes after midnight.</summary>
    public static bool TryParseTime(string? text, out int minuteOfDay)
    {
        minuteOfDay = 0;
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

        minuteOfDay = hour * 60 + minute;
        return true;
    }

    /// <summary>Instant of a local wall-clock time; a time skipped by a DST jump moves forward past the gap.</summary>
    private static DateTimeOffset StartOf(TimeZoneInfo zone, DateOnly day, int minuteOfDay)
    {
        DateTime local = day.ToDateTime(TimeOnly.MinValue).AddMinutes(minuteOfDay);
        while (zone.IsInvalidTime(local))
            local = local.AddMinutes(30);
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }

    private TimeZoneInfo? Zone()
    {
        if (_zone is null || _zone.Id != TimeZoneId)
            _zone = TimeZoneInfo.TryFindSystemTimeZoneById(TimeZoneId ?? "", out TimeZoneInfo? zone) ? zone : null;
        return _zone;
    }
}
