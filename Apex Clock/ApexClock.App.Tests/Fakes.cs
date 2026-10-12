using ApexClock.Core;
using Apex_Clock.Messages;
using Apex_Clock.Services;
using CommunityToolkit.Mvvm.Messaging;

namespace ApexClock.App.Tests;

/// <summary>Fixed clock in Paris time.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
}

/// <summary>In-memory store that records what is saved.</summary>
public sealed class FakeScheduleStore(IMessenger messenger) : IScheduleStore
{
    public RotationSchedule Current { get; private set; } = RotationSchedule.CreateDefault();

    public List<RotationSchedule> Saved { get; } = new List<RotationSchedule>();

    public string? Save(RotationSchedule schedule)
    {
        Saved.Add(schedule);
        Current = schedule;
        messenger.Send(new ScheduleChangedMessage(schedule));
        return null;
    }

    /// <summary>A change made elsewhere, e.g. by another editor.</summary>
    public void Replace(RotationSchedule schedule)
    {
        Current = schedule;
        messenger.Send(new ScheduleChangedMessage(schedule));
    }
}
