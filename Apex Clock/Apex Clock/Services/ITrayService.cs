namespace Apex_Clock.Services;

public interface ITrayService
{
    /// <summary>Tooltip of the notification area icon.</summary>
    void SetStatus(string text);
}
