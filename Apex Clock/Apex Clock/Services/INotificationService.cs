namespace Apex_Clock.Services;

public interface INotificationService
{
    /// <summary>Announces a new ranked map, with its artwork and the rotation chime.</summary>
    void ShowMapChange(string map, string title, string message);
}
