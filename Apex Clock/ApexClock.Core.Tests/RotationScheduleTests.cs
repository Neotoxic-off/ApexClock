using ApexClock.Core;

namespace ApexClock.Core.Tests;

public sealed class RotationScheduleTests
{
    /// <summary>Ranked rotation of 2026-10-12: Olympus from 10:00 Paris time, 4h30 per map.</summary>
    private static readonly RotationSchedule Ranked = RotationSchedule.CreateDefault();

    private static readonly DateTimeOffset TwelveForty = new DateTimeOffset(2026, 10, 12, 12, 40, 0, TimeSpan.FromHours(2));

    [Fact]
    public void GetState_returns_the_live_slot()
    {
        RotationState state = Ranked.GetState(TwelveForty)!;

        Assert.Equal("Olympus", state.CurrentMap);
        Assert.Equal("Broken Moon", state.NextMap);
        Assert.Equal(new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.FromHours(2)), state.StartsAt);
        Assert.Equal(new DateTimeOffset(2026, 10, 12, 14, 30, 0, TimeSpan.FromHours(2)), state.EndsAt);
    }

    [Fact]
    public void GetState_wraps_the_cycle_and_works_before_the_anchor()
    {
        // 13h30 cycles: Olympus is back at 23:30, and was live 13h30 before the anchor too.
        Assert.Equal("Olympus", Ranked.GetState(new DateTimeOffset(2026, 10, 13, 0, 0, 0, TimeSpan.FromHours(2)))!.CurrentMap);
        Assert.Equal("Olympus", Ranked.GetState(new DateTimeOffset(2026, 10, 11, 21, 0, 0, TimeSpan.FromHours(2)))!.CurrentMap);
    }

    [Fact]
    public void GetUpcoming_lists_the_following_slots()
    {
        IReadOnlyList<RotationState> upcoming = Ranked.GetUpcoming(TwelveForty, 4);

        Assert.Equal(["Broken Moon", "World's Edge", "Olympus", "Broken Moon"], upcoming.Select((RotationState s) => s.CurrentMap));
        Assert.Equal(new DateTimeOffset(2026, 10, 13, 4, 0, 0, TimeSpan.FromHours(2)), upcoming[3].StartsAt);
    }

    [Fact]
    public void SlotId_changes_on_every_step()
    {
        RotationState now = Ranked.GetState(TwelveForty)!;
        RotationState next = Ranked.GetState(now.EndsAt)!;

        Assert.NotEqual(now.SlotId, next.SlotId);
        Assert.Equal(now.SlotId, Ranked.GetState(now.EndsAt.AddTicks(-1))!.SlotId);
    }

    [Fact]
    public void CycleStartAt_is_the_first_entry_start()
    {
        DateTimeOffset afternoon = new DateTimeOffset(2026, 10, 12, 20, 0, 0, TimeSpan.FromHours(2));

        Assert.Equal(new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.FromHours(2)), Ranked.CycleStartAt(afternoon));
    }

    [Fact]
    public void Validate_rejects_unusable_schedules()
    {
        Assert.NotNull(new RotationSchedule { Anchor = Ranked.Anchor }.Validate());
        Assert.NotNull(new RotationSchedule { Anchor = Ranked.Anchor, Entries = [new RotationEntry { Map = " ", DurationMinutes = 60 }] }.Validate());
        Assert.NotNull(new RotationSchedule { Anchor = Ranked.Anchor, Entries = [new RotationEntry { Map = "Olympus", DurationMinutes = 0 }] }.Validate());
        Assert.NotNull(new RotationSchedule { Entries = [new RotationEntry { Map = "Olympus", DurationMinutes = 60 }] }.Validate());
        Assert.Null(Ranked.Validate());
    }
}
