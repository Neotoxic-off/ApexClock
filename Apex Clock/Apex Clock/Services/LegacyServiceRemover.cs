using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace Apex_Clock.Services;

/// <summary>
/// Removes the "ApexClock" Windows service of versions before 1.2, its files in Program Files and its data in
/// ProgramData. The work runs in an elevated copy of this exe (<see cref="RunElevated"/>), started through UAC.
/// </summary>
public sealed class LegacyServiceRemover : ILegacyServiceRemover
{
    public const string RemoveArgument = "--remove-legacy-service";

    private const string ServiceName = "ApexClock";
    private const int ErrorCancelled = 1223;

    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(30);

    private static string ProgramFilesDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ApexClock");

    private static string ProgramDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ApexClock");

    public bool IsInstalled()
    {
        using ServiceController? controller = Find();
        return controller is not null;
    }

    public async Task<string?> RemoveAsync()
    {
        string resultPath = Path.Combine(Path.GetTempPath(), $"ApexClock-setup-{Guid.NewGuid():N}.txt");
        ProcessStartInfo info = new ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute = true,
            Verb = "runas",
            Arguments = $"{RemoveArgument} \"{resultPath}\"",
        };

        try
        {
            using Process? process = Process.Start(info);
            if (process is null)
                return "Could not start the removal.";
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
                return null;
            string? error = File.Exists(resultPath) ? await File.ReadAllTextAsync(resultPath) : null;
            return string.IsNullOrWhiteSpace(error) ? $"Removal failed (exit code {process.ExitCode})." : error;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
        {
            return "Cancelled: administrator rights are required.";
        }
        finally
        {
            File.Delete(resultPath);
        }
    }

    /// <summary>Entry point of the elevated process. Writes the error (if any) to <paramref name="resultPath"/>.</summary>
    /// <returns>Process exit code: 0 on success.</returns>
    public static int RunElevated(string resultPath)
    {
        try
        {
            Remove();
            return 0;
        }
        catch (Exception ex)
        {
            try
            {
                File.WriteAllText(resultPath, ex.Message);
            }
            catch (IOException)
            {
            }
            return 1;
        }
    }

    private static void Remove()
    {
        using (ServiceController? controller = Find())
        {
            if (controller is not null)
            {
                if (controller.Status is not (ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending))
                    controller.Stop();
                controller.WaitForStatus(ServiceControllerStatus.Stopped, StopTimeout);
                RunSc("delete", ServiceName);
            }
        }

        // The stopped service process can hold its files for a moment.
        DeleteDirectory(ProgramFilesDirectory);
        DeleteDirectory(ProgramDataDirectory);
    }

    private static ServiceController? Find()
    {
        ServiceController controller = new ServiceController(ServiceName);
        try
        {
            _ = controller.Status;
            return controller;
        }
        catch (InvalidOperationException)
        {
            controller.Dispose();
            return null;
        }
    }

    private static void DeleteDirectory(string path)
    {
        const int attempts = 10;
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
                return;
            }
            catch (Exception ex) when (attempt < attempts && ex is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(500);
            }
        }
    }

    private static void RunSc(params string[] arguments)
    {
        ProcessStartInfo info = new ProcessStartInfo("sc.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };
        foreach (string argument in arguments)
            info.ArgumentList.Add(argument);

        using Process process = Process.Start(info)!;
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"sc {arguments[0]} failed: {output.Trim()}");
    }
}
