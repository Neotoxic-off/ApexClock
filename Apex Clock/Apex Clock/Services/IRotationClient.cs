using ApexClock.Core;

namespace Apex_Clock.Services;

/// <summary>
/// Connection to the ApexClock Windows service. Incoming data is published on the messenger
/// (<see cref="Messages.SnapshotReceivedMessage"/>, <see cref="Messages.ServiceErrorMessage"/>,
/// <see cref="Messages.ConnectionChangedMessage"/>) on the UI thread.
/// </summary>
public interface IRotationClient
{
    void Start();

    /// <summary>Returns false when the service is unreachable.</summary>
    Task<bool> SendScheduleAsync(RotationSchedule schedule);
}
