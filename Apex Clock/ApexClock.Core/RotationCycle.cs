namespace ApexClock.Core;

/// <summary>A map of the cycle and the local time it goes live.</summary>
public sealed record CycleSlot(string Map, ClockTime Start);

/// <summary>
/// One cycle of the rotation as wall-clock times, the way the game shows it: each map starts at its time and
/// lasts until the next one, the last one until <see cref="End"/>. A time earlier than the previous one is the next day.
/// </summary>
public sealed record RotationCycle(IReadOnlyList<CycleSlot> Slots, ClockTime End)
{
    /// <summary>How long each slot lasts, in minutes.</summary>
    public IReadOnlyList<int> Lengths() =>
        Slots.Select((CycleSlot slot, int i) => slot.Start.MinutesUntil(i + 1 < Slots.Count ? Slots[i + 1].Start : End)).ToList();

    /// <summary>The cycle running at <paramref name="now"/>, in <paramref name="zone"/> local times.</summary>
    public static RotationCycle? Running(RotationSchedule schedule, DateTimeOffset now, TimeZoneInfo zone)
    {
        DateTimeOffset? cycleStart = schedule.CycleStartAt(now);
        if (cycleStart is not DateTimeOffset at)
            return null;

        List<CycleSlot> slots = new List<CycleSlot>(schedule.Entries.Count);
        foreach (RotationEntry entry in schedule.Entries)
        {
            slots.Add(new CycleSlot(entry.Map, ClockTime.At(at, zone)));
            at = at.AddMinutes(entry.DurationMinutes);
        }
        return new RotationCycle(slots, ClockTime.At(at, zone));
    }

    /// <summary>
    /// Schedule repeating this cycle, anchored on the occurrence of the first start time whose cycle is running at
    /// <paramref name="now"/>, otherwise on the next one.
    /// </summary>
    public RotationSchedule ToSchedule(DateTimeOffset now, TimeZoneInfo zone)
    {
        if (Slots.Count == 0)
            throw new InvalidOperationException("A cycle needs at least one map.");

        IReadOnlyList<int> lengths = Lengths();
        return new RotationSchedule
        {
            Anchor = AnchorFor(Slots[0].Start, lengths.Sum(), now, zone),
            Entries = Slots.Select((CycleSlot slot, int i) => new RotationEntry { Map = slot.Map.Trim(), DurationMinutes = lengths[i] }).ToList(),
        };
    }

    public bool Equals(RotationCycle? other) =>
        other is not null && End == other.End && Slots.SequenceEqual(other.Slots);

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(End);
        foreach (CycleSlot slot in Slots)
            hash.Add(slot);
        return hash.ToHashCode();
    }

    private static DateTimeOffset AnchorFor(ClockTime firstStart, int cycleMinutes, DateTimeOffset now, TimeZoneInfo zone)
    {
        DateOnly today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
        int lookBack = cycleMinutes / ClockTime.MinutesPerDay + 1;
        DateTimeOffset? upcoming = null;
        for (int day = -lookBack; day <= 1; day++)
        {
            DateTimeOffset start = LocalInstant(zone, today.AddDays(day), firstStart);
            if (start <= now && start.AddMinutes(cycleMinutes) > now)
                return start.ToUniversalTime();
            upcoming ??= start > now ? start : null;
        }
        return upcoming!.Value.ToUniversalTime();
    }

    /// <summary>Instant of a local wall-clock time; a time skipped by a DST jump moves forward past the gap.</summary>
    private static DateTimeOffset LocalInstant(TimeZoneInfo zone, DateOnly day, ClockTime time)
    {
        DateTime local = day.ToDateTime(new TimeOnly(time.Hour, time.Minute));
        while (zone.IsInvalidTime(local))
            local = local.AddMinutes(30);
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }
}
