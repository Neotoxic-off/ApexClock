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

/// <summary>
/// Edits the rotation as the times of its current cycle. The editor is dirty while its cycle differs from the
/// saved one; a clean editor follows the saved schedule.
/// </summary>
public sealed partial class RotationEditorViewModel : ObservableRecipient,
    IRecipient<ScheduleChangedMessage>,
    IDisposable
{
    private const int DefaultLengthMinutes = 270;

    private readonly IScheduleStore _store;
    private readonly TimeProvider _time;
    private RotationCycle? _saved;

    public RotationEditorViewModel(IScheduleStore store, TimeProvider time, IMessenger messenger)
        : base(messenger)
    {
        _store = store;
        _time = time;
        StatusMessage = "";
        CycleEndText = "";

        Entries.CollectionChanged += OnEntriesChanged;
        LoadSavedCycle();
        IsActive = true;
    }

    public ObservableCollection<EntryViewModel> Entries { get; } = new ObservableCollection<EntryViewModel>();

    public IReadOnlyList<string> KnownMaps => RotationSchedule.KnownMaps;

    [ObservableProperty]
    public partial string StatusMessage { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(RevertCommand))]
    public partial bool IsDirty { get; private set; }

    /// <summary>Local time the last map ends and the cycle restarts from the first one.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCycleEndValid))]
    public partial string CycleEndText { get; set; }

    public bool IsCycleEndValid => ClockTime.TryParse(CycleEndText, out ClockTime _);

    partial void OnCycleEndTextChanged(string value) => OnDraftChanged();

    public void Receive(ScheduleChangedMessage message)
    {
        if (!IsDirty)
            LoadSavedCycle();
    }

    [RelayCommand]
    private void Add()
    {
        // The new map takes over at the current cycle end, which moves 4h30 later.
        ClockTime start = ClockTime.TryParse(CycleEndText, out ClockTime end) ? end : default;
        Entries.Add(new EntryViewModel(RotationSchedule.KnownMaps[0], start));
        CycleEndText = start.AddMinutes(DefaultLengthMinutes).ToString();
    }

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove(EntryViewModel? entry)
    {
        if (entry is not null)
            Entries.Remove(entry);
    }

    private bool CanRemove(EntryViewModel? entry) => entry is not null && Entries.Count > 1;

    [RelayCommand(CanExecute = nameof(IsDirty))]
    private void Save()
    {
        RotationCycle? draft = Draft();
        if (draft is null)
        {
            int invalidRow = Entries.ToList().FindIndex((EntryViewModel e) => !e.IsStartValid);
            StatusMessage = invalidRow >= 0
                ? $"Row {invalidRow + 1}: enter a start time like 19:00 or 19h30."
                : "Enter when the cycle repeats, like 23:30.";
            return;
        }

        string? error = _store.Save(draft.ToSchedule(_time.GetUtcNow(), _time.LocalTimeZone));
        if (error is not null)
        {
            StatusMessage = error;
            return;
        }

        // Still dirty while the store announced the change, so the rows were kept: they are now the saved cycle.
        _saved = draft;
        OnDraftChanged();
        StatusMessage = "Rotation saved";
    }

    [RelayCommand(CanExecute = nameof(IsDirty))]
    private void Revert()
    {
        LoadSavedCycle();
        StatusMessage = "";
    }

    /// <summary>Shows the cycle running now according to the saved schedule.</summary>
    private void LoadSavedCycle()
    {
        _saved = RotationCycle.Running(_store.Current, _time.GetUtcNow(), _time.LocalTimeZone);

        // Clear() reports no removed items, so unhook the rows first.
        foreach (EntryViewModel entry in Entries)
            entry.PropertyChanged -= OnEntryChanged;
        Entries.Clear();
        foreach (CycleSlot slot in _saved?.Slots ?? [])
            Entries.Add(new EntryViewModel(slot.Map, slot.Start));
        CycleEndText = _saved?.End.ToString() ?? "";
        OnDraftChanged();
    }

    /// <summary>The cycle as typed, or null while a time is missing or invalid.</summary>
    private RotationCycle? Draft()
    {
        List<CycleSlot?> slots = Entries.Select((EntryViewModel e) => e.ToSlot()).ToList();
        if (slots.Count == 0 || slots.Contains(null) || !ClockTime.TryParse(CycleEndText, out ClockTime end))
            return null;
        return new RotationCycle(slots.OfType<CycleSlot>().ToList(), end);
    }

    private void OnDraftChanged()
    {
        RotationCycle? draft = Draft();
        IReadOnlyList<int>? lengths = draft?.Lengths();
        for (int i = 0; i < Entries.Count; i++)
            Entries[i].ShowLength(lengths?[i]);

        IsDirty = _saved is null ? Entries.Count > 0 : !_saved.Equals(draft);
    }

    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (EntryViewModel entry in e.OldItems ?? Array.Empty<EntryViewModel>())
            entry.PropertyChanged -= OnEntryChanged;
        foreach (EntryViewModel entry in e.NewItems ?? Array.Empty<EntryViewModel>())
            entry.PropertyChanged += OnEntryChanged;

        RemoveCommand.NotifyCanExecuteChanged();
        OnDraftChanged();
    }

    private void OnEntryChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EntryViewModel.Map) or nameof(EntryViewModel.StartText))
            OnDraftChanged();
    }

    public void Dispose() => IsActive = false;
}
