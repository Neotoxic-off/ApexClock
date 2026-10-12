using Apex_Clock.Services;
using CommunityToolkit.Mvvm.Input;

namespace Apex_Clock.ViewModels;

/// <summary>The compact companion window: the live rotation and a way into the settings.</summary>
public sealed partial class MainViewModel(LiveRotationViewModel live, ISettingsWindowService settingsWindow)
{
    public LiveRotationViewModel Live { get; } = live;

    [RelayCommand]
    private void OpenSettings() => settingsWindow.Show();
}
