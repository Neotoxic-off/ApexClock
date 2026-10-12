using ApexClock.Core;
using Apex_Clock.Formatting;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apex_Clock.ViewModels;

/// <summary>Editable row of the rotation: a map and the local time it goes live in the current cycle.</summary>
public sealed partial class EntryViewModel : ObservableObject
{
    public EntryViewModel(string map, ClockTime start)
    {
        Map = map;
        StartText = start.ToString();
        Length = "";
    }

    [ObservableProperty]
    public partial string Map { get; set; }

    /// <summary>Start time as typed: "19:00", "19h", "19h30"…</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Start))]
    [NotifyPropertyChangedFor(nameof(IsStartValid))]
    public partial string StartText { get; set; }

    /// <summary>How long the map stays live, e.g. "4h30"; empty while the cycle has an invalid time.</summary>
    [ObservableProperty]
    public partial string Length { get; private set; }

    public ClockTime? Start => ClockTime.TryParse(StartText, out ClockTime start) ? start : null;

    public bool IsStartValid => Start is not null;

    public void ShowLength(int? minutes) => Length = minutes is int value ? DurationText.Compact(value) : "";

    /// <summary>The row as a slot of the cycle, or null while its time is invalid.</summary>
    public CycleSlot? ToSlot() => Start is ClockTime start ? new CycleSlot(Map.Trim(), start) : null;
}
