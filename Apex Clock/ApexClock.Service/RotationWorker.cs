using ApexClock.Core;

namespace ApexClock.Service;

/// <summary>Pushes a snapshot to clients whenever the rotation advances or the schedule is replaced.</summary>
public sealed class RotationWorker(ScheduleStore store, ClientHub hub, ILogger<RotationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RotationSchedule? lastSchedule = null;
        long? lastSlot = null;

        // Polling (rather than sleeping until EndsAt) stays correct across sleep/resume and clock changes.
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        do
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            RotationSchedule schedule = store.Current;
            RotationState? state = schedule.GetState(now);

            if (ReferenceEquals(schedule, lastSchedule) && state?.SlotId == lastSlot)
                continue;

            if (state is not null)
                logger.LogInformation("Ranked map: {Map} until {EndsAt:u}, next {Next}.", state.CurrentMap, state.EndsAt, state.NextMap);

            lastSchedule = schedule;
            lastSlot = state?.SlotId;
            await hub.BroadcastAsync(PipeMessage.Snapshot(schedule, now), stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
