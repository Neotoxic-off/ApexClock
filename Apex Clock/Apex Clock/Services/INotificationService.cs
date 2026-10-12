namespace Apex_Clock.Services;

public interface INotificationService
{
    void ShowNotification(string title, string message);

    void SetStatus(string text);
}
