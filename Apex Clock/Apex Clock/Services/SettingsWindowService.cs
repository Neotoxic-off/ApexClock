using System.Windows;
using Apex_Clock.ViewModels;
using Apex_Clock.Views;

namespace Apex_Clock.Services;

/// <summary>Keeps a single settings window; a closed one is created again on the next request.</summary>
public sealed class SettingsWindowService(SettingsWindowViewModel viewModel) : ISettingsWindowService
{
    private SettingsWindow? _window;

    public void Show()
    {
        if (_window is null)
        {
            _window = new SettingsWindow(viewModel);
            _window.Closed += (object? sender, EventArgs e) => _window = null;
        }

        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
            _window.WindowState = WindowState.Normal;
        _window.Activate();
    }
}
