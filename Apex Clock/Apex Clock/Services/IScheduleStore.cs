using ApexClock.Core;

namespace Apex_Clock.Services;

/// <summary>The rotation schedule, kept on disk. Saving publishes a <see cref="Messages.ScheduleChangedMessage"/>.</summary>
public interface IScheduleStore
{
    RotationSchedule Current { get; }

    /// <summary>Normalizes, validates and stores <paramref name="schedule"/>. Returns an error message, or null on success.</summary>
    string? Save(RotationSchedule schedule);
}
