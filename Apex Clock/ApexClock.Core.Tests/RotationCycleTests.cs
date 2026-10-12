using ApexClock.Core;

namespace ApexClock.Core.Tests;

public sealed class RotationCycleTests
{
    private static readonly TimeZoneInfo Paris = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");

    private static readonly RotationCycle Ranked = new RotationCycle(
        [
            new CycleSlot("Olympus", new ClockTime(10 * 60)),
            new CycleSlot("Broken Moon", new ClockTime(14 * 60 + 30)),
            new CycleSlot("World's Edge", new ClockTime(19 * 60)),
        ],
        new ClockTime(23 * 60 + 30));

    private static DateTimeOffset ParisTime(int day, int hour, int minute) =>
        new DateTimeOffset(2026, 10, day, hour, minute, 0, TimeSpan.FromHours(2));

    [Fact]
    public void Lengths_run_to_the_next_start_and_the_cycle_end()
    {
        Assert.Equal([270, 270, 270], Ranked.Lengths());
    }

    [Fact]
    public void Lengths_cross_midnight()
    {
        RotationCycle overnight = new RotationCycle(
            [new CycleSlot("Olympus", new ClockTime(23 * 60 + 30)), new CycleSlot("Broken Moon", new ClockTime(4 * 60))],
            new ClockTime(8 * 60 + 30));

        Assert.Equal([270, 270], overnight.Lengths());
    }

    [Fact]
    public void ToSchedule_anchors_on_the_running_cycle()
    {
        RotationSchedule schedule = Ranked.ToSchedule(ParisTime(12, 12, 40), Paris);

        Assert.Equal(RotationSchedule.CreateDefault().Anchor, schedule.Anchor);
        Assert.Equal([270, 270, 270], schedule.Entries.Select((RotationEntry e) => e.DurationMinutes));
    }

    [Fact]
    public void ToSchedule_finds_a_cycle_that_started_yesterday()
    {
        RotationCycle overnight = new RotationCycle(
            [new CycleSlot("Olympus", new ClockTime(23 * 60 + 30)), new CycleSlot("Broken Moon", new ClockTime(4 * 60))],
            new ClockTime(8 * 60 + 30));

        RotationSchedule schedule = overnight.ToSchedule(ParisTime(13, 1, 0), Paris);

        Assert.Equal(ParisTime(12, 23, 30), schedule.Anchor);
    }

    [Fact]
    public void ToSchedule_takes_the_next_cycle_when_none_is_running()
    {
        RotationSchedule schedule = Ranked.ToSchedule(ParisTime(12, 9, 0), Paris);

        Assert.Equal(ParisTime(12, 10, 0), schedule.Anchor);
    }

    [Fact]
    public void Running_round_trips_with_ToSchedule()
    {
        DateTimeOffset now = ParisTime(12, 12, 40);
        RotationSchedule schedule = Ranked.ToSchedule(now, Paris);

        Assert.Equal(Ranked, RotationCycle.Running(schedule, now, Paris));
    }

    [Fact]
    public void Running_follows_the_drift_of_later_cycles()
    {
        RotationCycle tomorrow = RotationCycle.Running(RotationSchedule.CreateDefault(), ParisTime(13, 14, 0), Paris)!;

        Assert.Equal(["13:00", "17:30", "22:00"], tomorrow.Slots.Select((CycleSlot s) => s.Start.ToString()));
        Assert.Equal("02:30", tomorrow.End.ToString());
    }

    [Fact]
    public void Equality_compares_slots_and_end()
    {
        RotationCycle copy = new RotationCycle(Ranked.Slots.ToList(), Ranked.End);
        RotationCycle renamed = new RotationCycle([new CycleSlot("Kings Canyon", new ClockTime(600)), .. Ranked.Slots.Skip(1)], Ranked.End);

        Assert.Equal(Ranked, copy);
        Assert.Equal(Ranked.GetHashCode(), copy.GetHashCode());
        Assert.NotEqual(Ranked, renamed);
        Assert.NotEqual(Ranked, new RotationCycle(Ranked.Slots, new ClockTime(0)));
    }
}
