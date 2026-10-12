namespace Apex_Clock.Services;

public enum ServiceInstallState
{
    NotInstalled,
    Stopped,
    Starting,
    Running,
    Stopping,
}

/// <summary>Installs and removes the ApexClock Windows service (asks for administrator rights).</summary>
public interface IServiceInstaller
{
    ServiceInstallState GetState();

    /// <summary>Installs (or updates) and starts the service. Returns an error message, or null on success.</summary>
    Task<string?> InstallAsync();

    /// <summary>Stops and removes the service. Returns an error message, or null on success.</summary>
    Task<string?> UninstallAsync();
}
