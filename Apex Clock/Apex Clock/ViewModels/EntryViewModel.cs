using ApexClock.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apex_Clock.ViewModels;

/// <summary>Editable row of the rotation.</summary>
public sealed partial class EntryViewModel : ObservableObject
{
    public EntryViewModel(string map, int durationMinutes)
    {
        Map = map;
        Hours = durationMinutes / 60;
        Minutes = durationMinutes % 60;
    }

    [ObservableProperty]
    public partial string Map { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalMinutes))]
    public partial int Hours { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalMinutes))]
    public partial int Minutes { get; set; }

    public int TotalMinutes => Hours * 60 + Minutes;

    public static EntryViewModel From(RotationEntry entry) => new EntryViewModel(entry.Map, entry.DurationMinutes);

    public RotationEntry ToEntry() => new RotationEntry { Map = Map.Trim(), DurationMinutes = TotalMinutes };
}
