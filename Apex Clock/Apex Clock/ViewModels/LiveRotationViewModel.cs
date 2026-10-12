using System.Collections.ObjectModel;
using ApexClock.Core;
using Apex_Clock.Formatting;
using Apex_Clock.Messages;
using Apex_Clock.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace Apex_Clock.ViewModels;

/// <summary>The live map, its countdown and the slots coming next.</summary>
public sealed partial class LiveRotationViewModel : ObservableRecipient,
    IRecipient<ClockTickMessage>,
    IDisposable
{
    private const string NoMap = "—";
    private const int UpcomingCount = 3;

    private readonly IScheduleStore _store;
    private RotationSchedule? _upcomingSchedule;
    private long? _upcomingSlot;

    public LiveRotationViewModel(IScheduleStore store, TimeProvider time, IMessenger messenger)
        : base(messenger)
    {
        _store = store;
        CurrentMap = NoMap;
        NextMap = NoMap;
        TimeRange = "";
        Remaining = "";
        Countdown = "";
        Refresh(time.GetUtcNow());
        IsActive = true;
    }

    public ObservableCollection<UpcomingSlotViewModel> Upcoming { get; } = new ObservableCollection<UpcomingSlotViewModel>();

    [ObservableProperty]
    public partial string CurrentMap { get; private set; }

    [ObservableProperty]
    public partial string NextMap { get; private set; }

    /// <summary>"From 10:00 to 14:30".</summary>
    [ObservableProperty]
    public partial string TimeRange { get; private set; }

    /// <summary>"ends in 3 hours, 55 mins".</summary>
    [ObservableProperty]
    public partial string Remaining { get; private set; }

    /// <summary>"03:55:12".</summary>
    [ObservableProperty]
    public partial string Countdown { get; private set; }

    /// <summary>Elapsed share of the current slot, 0 to 100.</summary>
    [ObservableProperty]
    public partial double Progress { get; private set; }

    public void Receive(ClockTickMessage message) => Refresh(message.Value);

    private void Refresh(DateTimeOffset now)
    {
        RotationSchedule schedule = _store.Current;
        RotationState? state = schedule.GetState(now);
        if (state is null)
        {
            CurrentMap = NoMap;
            NextMap = NoMap;
            TimeRange = "";
            Remaining = "";
            Countdown = "--:--:--";
            Progress = 0;
            Upcoming.Clear();
            _upcomingSchedule = null;
            return;
        }

        CurrentMap = state.CurrentMap;
        NextMap = state.NextMap;
        TimeRange = RotationText.TimeRange(state);
        Remaining = RotationText.Remaining(state, now);
        Countdown = DurationText.Countdown(state.EndsAt - now);
        Progress = 100 * RotationText.Progress(state, now);

        // Rebuild the list only when the rotation moved on or the schedule changed.
        if (!ReferenceEquals(schedule, _upcomingSchedule) || state.SlotId != _upcomingSlot)
        {
            _upcomingSchedule = schedule;
            _upcomingSlot = state.SlotId;
            Upcoming.Clear();
            foreach (RotationState slot in schedule.GetUpcoming(now, UpcomingCount))
                Upcoming.Add(new UpcomingSlotViewModel(slot));
        }
        foreach (UpcomingSlotViewModel slot in Upcoming)
            slot.Refresh(now);
    }

    public void Dispose() => IsActive = false;
}
