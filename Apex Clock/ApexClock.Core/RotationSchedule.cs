using System.Diagnostics;

namespace ApexClock.Core;

public sealed class RotationEntry
{
    public string Map { get; set; } = "";
    public int DurationMinutes { get; set; }
}

/// <summary>Position in the rotation at a given instant.</summary>
/// <param name="SlotId">Unique per (cycle, entry) pair; changes every time the rotation advances.</param>
public sealed record RotationState(
    int Index,
    long SlotId,
    string CurrentMap,
    string NextMap,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);

/// <summary>
/// Repeating ranked map rotation: entries play back to back starting at <see cref="Anchor"/>,
/// then the cycle restarts from the first entry. The cycle need not last a day, so start times drift from day to day.
/// </summary>
public sealed class RotationSchedule
{
    public const int MaxEntries = 50;
    public const int MaxMapNameLength = 64;
    public const int MaxDurationMinutes = 14 * 24 * 60;

    public static readonly IReadOnlyList<string> KnownMaps =
    [
        "Kings Canyon",
        "World's Edge",
        "Olympus",
        "Storm Point",
        "Broken Moon",
        "E-District",
    ];

    /// <summary>Instant at which the first entry of a cycle started.</summary>
    public DateTimeOffset Anchor { get; set; }

    public List<RotationEntry> Entries { get; set; } = [];

    /// <summary>Ranked rotation as of 2026-10-12: Olympus started at 10:00 Paris time, 4 h 30 per map.</summary>
    public static RotationSchedule CreateDefault() => new RotationSchedule
    {
        Anchor = new DateTimeOffset(2026, 10, 12, 8, 0, 0, TimeSpan.Zero),
        Entries =
        [
            new RotationEntry { Map = "Olympus", DurationMinutes = 270 },
            new RotationEntry { Map = "Broken Moon", DurationMinutes = 270 },
            new RotationEntry { Map = "World's Edge", DurationMinutes = 270 },
        ],
    };

    /// <summary>Returns an error message, or null when the schedule is usable.</summary>
    public string? Validate()
    {
        if (Entries is null || Entries.Count == 0)
            return "The rotation needs at least one map.";
        if (Entries.Count > MaxEntries)
            return $"The rotation can hold at most {MaxEntries} maps.";
        if (Anchor.Year is < 2000 or > 2100)
            return "Invalid rotation start date.";

        for (int i = 0; i < Entries.Count; i++)
        {
            RotationEntry? entry = Entries[i];
            if (entry is null || string.IsNullOrWhiteSpace(entry.Map))
                return $"Row {i + 1}: map name is empty.";
            if (entry.Map.Length > MaxMapNameLength)
                return $"Row {i + 1}: map name is too long.";
            if (entry.DurationMinutes is < 1 or > MaxDurationMinutes)
                return $"Row {i + 1}: duration must be between 1 minute and {MaxDurationMinutes / 60 / 24} days.";
        }

        return null;
    }

    public RotationSchedule Normalized() => new RotationSchedule
    {
        Anchor = Anchor.ToUniversalTime(),
        Entries = (Entries ?? [])
            .Select((RotationEntry? e) => new RotationEntry { Map = e?.Map?.Trim() ?? "", DurationMinutes = e?.DurationMinutes ?? 0 })
            .ToList(),
    };

    public RotationState? GetState(DateTimeOffset now)
    {
        if (Validate() is not null)
            return null;

        long cycleTicks = Entries.Sum((RotationEntry e) => (long)e.DurationMinutes) * TimeSpan.TicksPerMinute;
        long elapsed = (now - Anchor).Ticks;

        // Floor division so instants before the anchor still map onto the cycle.
        long cycle = elapsed / cycleTicks;
        if (elapsed % cycleTicks < 0)
            cycle--;

        long offset = elapsed - cycle * cycleTicks;
        DateTimeOffset cycleStart = Anchor.AddTicks(cycle * cycleTicks);

        long accumulated = 0;
        for (int i = 0; i < Entries.Count; i++)
        {
            long duration = Entries[i].DurationMinutes * TimeSpan.TicksPerMinute;
            if (offset < accumulated + duration)
            {
                return new RotationState(
                    i,
                    cycle * Entries.Count + i,
                    Entries[i].Map,
                    Entries[(i + 1) % Entries.Count].Map,
                    cycleStart.AddTicks(accumulated),
                    cycleStart.AddTicks(accumulated + duration));
            }
            accumulated += duration;
        }

        throw new UnreachableException();
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

    /// <summary>Start of the cycle running at <paramref name="now"/>.</summary>
    public DateTimeOffset? CycleStartAt(DateTimeOffset now)
    {
        RotationState? state = GetState(now);
        if (state is null)
            return null;
        long before = Entries.Take(state.Index).Sum((RotationEntry e) => (long)e.DurationMinutes);
        return state.StartsAt.AddMinutes(-before);
    }
}
