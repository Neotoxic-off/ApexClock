using Microsoft.Win32;

namespace Apex_Clock.Services;

/// <summary>Per-user Run key entry, launching straight to the tray.</summary>
public sealed class AutoStartService : IAutoStartService
{
    public const string MinimizedArgument = "--minimized";

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ApexClock";

    public bool IsEnabled
    {
        get
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
    }

    public void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
            key.SetValue(ValueName, $"\"{Environment.ProcessPath}\" {MinimizedArgument}");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
