using ApexClock.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apex_Clock.ViewModels;

/// <summary>Editable row of the rotation: a map and the local time it goes live in the current cycle.</summary>
public sealed partial class EntryViewModel : ObservableObject
{
    public EntryViewModel(string map, int startMinute)
    {
        Map = map;
        StartText = RotationSchedule.FormatTime(startMinute);
        Length = "";
    }

    [ObservableProperty]
    public partial string Map { get; set; }

    /// <summary>Start time as typed: "19:00", "19h", "19h30"…</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStartValid))]
    public partial string StartText { get; set; }

    /// <summary>How long the map stays live, e.g. "4h30"; empty when it cannot be computed.</summary>
    [ObservableProperty]
    public partial string Length { get; private set; }

    public bool IsStartValid => RotationSchedule.TryParseTime(StartText, out int _);

    public int? StartMinute => RotationSchedule.TryParseTime(StartText, out int minute) ? minute : null;

    public void SetLength(int? minutes) => Length = minutes is int m ? (m % 60 == 0 ? $"{m / 60}h" : $"{m / 60}h{m % 60:00}") : "";

    public RotationEntry ToEntry(int durationMinutes) => new RotationEntry { Map = Map.Trim(), DurationMinutes = durationMinutes };
}
