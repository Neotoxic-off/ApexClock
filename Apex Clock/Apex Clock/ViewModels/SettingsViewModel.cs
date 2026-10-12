using Apex_Clock.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apex_Clock.ViewModels;

/// <summary>App preferences.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IAutoStartService _autoStart;

    public SettingsViewModel(IAutoStartService autoStart)
    {
        _autoStart = autoStart;
        StartWithWindows = autoStart.IsEnabled;
    }

    [ObservableProperty]
    public partial bool StartWithWindows { get; set; }

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (value != _autoStart.IsEnabled)
            _autoStart.SetEnabled(value);
    }
}
