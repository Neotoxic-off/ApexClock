namespace Apex_Clock.Services;

/// <summary>"Start with Windows" for the current user.</summary>
public interface IAutoStartService
{
    bool IsEnabled { get; }

    void SetEnabled(bool enabled);
}
