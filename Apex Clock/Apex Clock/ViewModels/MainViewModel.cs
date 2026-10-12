using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using ApexClock.Core;
using Apex_Clock.Messages;
using Apex_Clock.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Apex_Clock.ViewModels;

public sealed partial class MainViewModel : ObservableRecipient,
    IRecipient<SnapshotReceivedMessage>,
    IRecipient<ServiceErrorMessage>,
    IRecipient<ConnectionChangedMessage>,
    IDisposable
{
    private const string NoMap = "—";
    private const int UpcomingCount = 4;
    private const int DefaultLengthMinutes = 270;
    private static readonly TimeSpan ClockInterval = TimeSpan.FromSeconds(1);

    private readonly IRotationClient _client;
    private readonly INotificationService _notifications;
    private readonly ITrayService _tray;
    private readonly IAutoStartService _autoStart;
    private readonly CancellationTokenSource _clockCts = new CancellationTokenSource();

    private RotationSchedule? _schedule;
    private RotationSchedule? _upcomingSchedule;
    private long? _upcomingSlot;
    private string? _lastMap;
    private bool _loadingEditor;

    /// <remarks>Must be created on the UI thread: the clock loop resumes on the captured context.</remarks>
    public MainViewModel(
        IRotationClient client,
        INotificationService notifications,
        ITrayService tray,
        IAutoStartService autoStart,
        ServiceSetupViewModel serviceSetup,
        IMessenger messenger)
        : base(messenger)
    {
        ServiceSetup = serviceSetup;
        _client = client;
        _notifications = notifications;
        _tray = tray;
        _autoStart = autoStart;

        CurrentMap = NoMap;
        NextMap = NoMap;
        TimeRange = "";
        Remaining = "";
        Countdown = "--:--:--";
        StatusMessage = "Connecting to the service…";
        StartWithWindows = autoStart.IsEnabled;

        Entries.CollectionChanged += OnEntriesChanged;
        IsActive = true;
        _ = RunClockAsync(_clockCts.Token);
    }

    public ServiceSetupViewModel ServiceSetup { get; }

    public ObservableCollection<EntryViewModel> Entries { get; } = new ObservableCollection<EntryViewModel>();

    public ObservableCollection<UpcomingSlotViewModel> Upcoming { get; } = new ObservableCollection<UpcomingSlotViewModel>();

    public IReadOnlyList<string> KnownMaps => RotationSchedule.KnownMaps;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial bool IsConnected { get; private set; }

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

    [ObservableProperty]
    public partial string StatusMessage { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevertCommand))]
    public partial bool IsDirty { get; private set; }

    [ObservableProperty]
    public partial bool StartWithWindows { get; set; }

    public void Receive(SnapshotReceivedMessage message)
    {
        PipeMessage snapshot = message.Value;
        if (snapshot.Schedule is null)
            return;

        _schedule = snapshot.Schedule;
        RotationState? state = snapshot.State;

        // The service pushes on every rotation step; notify only when the map actually differs.
        if (state is not null)
        {
            if (_lastMap is not null && state.CurrentMap != _lastMap)
            {
                _notifications.ShowMapChange(
                    state.CurrentMap,
                    $"Ranked map: {state.CurrentMap}",
                    $"Until {state.EndsAt.ToLocalTime():HH:mm} · Next: {state.NextMap}");
            }
            _lastMap = state.CurrentMap;
        }

        if (!IsDirty)
            LoadEditor();
        UpdateClock();
    }

    public void Receive(ServiceErrorMessage message) => StatusMessage = message.Value;

    public void Receive(ConnectionChangedMessage message) => IsConnected = message.Value;

    partial void OnIsConnectedChanged(bool value) =>
        StatusMessage = value ? "Connected to the service" : "Service unreachable, retrying…";

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (value != _autoStart.IsEnabled)
            _autoStart.SetEnabled(value);
    }

    [RelayCommand]
    private void Add()
    {
        // Default to 4h30 after the latest start so the new row does not collide with an existing one.
        int last = Entries.Select((EntryViewModel e) => e.StartMinute ?? -1).DefaultIfEmpty(-1).Max();
        int start = last < 0 ? 0 : (last + DefaultLengthMinutes) % RotationSchedule.MinutesPerDay;
        Entries.Add(new EntryViewModel(RotationSchedule.KnownMaps[0], start));
    }

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove(EntryViewModel? entry)
    {
        if (entry is not null)
            Entries.Remove(entry);
    }

    private bool CanRemove(EntryViewModel? entry) => entry is not null && Entries.Count > 1;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        int invalid = Entries.ToList().FindIndex((EntryViewModel e) => !e.IsStartValid);
        if (invalid >= 0)
        {
            StatusMessage = $"Row {invalid + 1}: enter a start time like 19:00 or 19h30.";
            return;
        }

        RotationSchedule schedule = new RotationSchedule
        {
            TimeZoneId = TimeZoneInfo.Local.Id,
            Entries = Entries.Select((EntryViewModel e) => e.ToEntry()).ToList(),
        };

        string? error = schedule.Validate();
        if (error is not null)
        {
            StatusMessage = error;
            return;
        }

        if (!await _client.SendScheduleAsync(schedule))
        {
            StatusMessage = "Could not send: service unreachable.";
            return;
        }

        // The service answers with a snapshot, which reloads the editor (sorted by time) once it is clean.
        IsDirty = false;
        StatusMessage = "Rotation saved";
    }

    private bool CanSave() => IsConnected && IsDirty;

    [RelayCommand(CanExecute = nameof(CanRevert))]
    private void Revert() => LoadEditor();

    private bool CanRevert() => IsDirty && _schedule is not null;

    private async Task RunClockAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(ClockInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
                UpdateClock();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void UpdateClock()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        RotationState? state = _schedule?.GetState(now);
        if (_schedule is null || state is null)
        {
            CurrentMap = NoMap;
            NextMap = NoMap;
            TimeRange = "";
            Remaining = "";
            Countdown = "--:--:--";
            Progress = 0;
            Upcoming.Clear();
            _tray.SetStatus("Apex Clock");
            return;
        }

        TimeSpan left = state.EndsAt - now;
        TimeSpan length = state.EndsAt - state.StartsAt;

        CurrentMap = state.CurrentMap;
        NextMap = state.NextMap;
        TimeRange = $"From {state.StartsAt.ToLocalTime():HH:mm} to {state.EndsAt.ToLocalTime():HH:mm}";
        Remaining = $"ends in {DurationText.Format(left)}";
        Countdown = DurationText.Countdown(left);
        Progress = length > TimeSpan.Zero ? 100 * (1 - left / length) : 0;

        if (!ReferenceEquals(_schedule, _upcomingSchedule) || state.SlotId != _upcomingSlot)
        {
            _upcomingSchedule = _schedule;
            _upcomingSlot = state.SlotId;
            Upcoming.Clear();
            foreach (RotationState slot in _schedule.GetUpcoming(now, UpcomingCount))
                Upcoming.Add(new UpcomingSlotViewModel(slot));
        }
        foreach (UpcomingSlotViewModel slot in Upcoming)
            slot.Refresh(now);

        _tray.SetStatus($"Ranked: {state.CurrentMap}\n{TimeRange}, {Remaining}");
    }

    private void LoadEditor()
    {
        if (_schedule is null)
            return;

        _loadingEditor = true;
        try
        {
            Entries.Clear();
            foreach (RotationEntry entry in _schedule.Entries.OrderBy((RotationEntry e) => e.StartMinute))
                Entries.Add(EntryViewModel.From(entry));
            IsDirty = false;
        }
        finally
        {
            _loadingEditor = false;
        }
    }

    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (EntryViewModel entry in e.OldItems ?? Array.Empty<EntryViewModel>())
            entry.PropertyChanged -= OnEntryChanged;
        foreach (EntryViewModel entry in e.NewItems ?? Array.Empty<EntryViewModel>())
            entry.PropertyChanged += OnEntryChanged;

        RemoveCommand.NotifyCanExecuteChanged();
        RefreshLengths();
        MarkDirty();
    }

    private void OnEntryChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EntryViewModel.Length))
            return;
        if (e.PropertyName == nameof(EntryViewModel.StartText))
            RefreshLengths();
        MarkDirty();
    }

    /// <summary>Each map lasts until the next start time, the latest one wrapping to the next day.</summary>
    private void RefreshLengths()
    {
        List<int> starts = Entries
            .Select((EntryViewModel e) => e.StartMinute)
            .OfType<int>()
            .Distinct()
            .Order()
            .ToList();

        foreach (EntryViewModel entry in Entries)
        {
            if (entry.StartMinute is not int start)
            {
                entry.SetLength(null);
                continue;
            }
            int index = starts.IndexOf(start);
            int next = starts[(index + 1) % starts.Count];
            int length = next > start ? next - start : next - start + RotationSchedule.MinutesPerDay;
            entry.SetLength(length);
        }
    }

    private void MarkDirty()
    {
        if (!_loadingEditor)
            IsDirty = true;
    }

    public void Dispose()
    {
        _clockCts.Cancel();
        IsActive = false;
    }
}
