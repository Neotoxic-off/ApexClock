namespace Apex_Clock.Services;

public interface ITrayService
{
    /// <summary>Tooltip of the notification area icon.</summary>
    void SetStatus(string text);

    /// <summary>
    /// Shows the live map in the icon: its initials inside a ring filled by <paramref name="progress"/> (0 to 1).
    /// A null map restores the app icon.
    /// </summary>
    void SetLiveMap(string? map, double progress);
}
