namespace Apex_Clock.Services;

/// <summary>Removes the Windows service that versions before 1.2 installed (asks for administrator rights).</summary>
public interface ILegacyServiceRemover
{
    bool IsInstalled();

    /// <summary>Stops and deletes the service and its files. Returns an error message, or null on success.</summary>
    Task<string?> RemoveAsync();
}
