using ApexClock.Core;
using Apex_Clock.Formatting;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apex_Clock.ViewModels;

/// <summary>A future slot of the rotation, as shown in the "Up next" list.</summary>
public sealed partial class UpcomingSlotViewModel : ObservableObject
{
    public UpcomingSlotViewModel(RotationState slot)
    {
        Map = slot.CurrentMap;
        StartsAt = slot.StartsAt;
        TimeRange = RotationText.ShortTimeRange(slot);
        StartsIn = "";
    }

    public string Map { get; }

    public DateTimeOffset StartsAt { get; }

    public string TimeRange { get; }

    [ObservableProperty]
    public partial string StartsIn { get; private set; }

    public void Refresh(DateTimeOffset now) => StartsIn = $"in {DurationText.Format(StartsAt - now)}";
}
