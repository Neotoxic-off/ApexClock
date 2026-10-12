using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace Apex_Clock.Services;

/// <summary>
/// Installs the service bundled next to the app (Service folder) into Program Files.
/// The work runs in an elevated copy of this exe (<see cref="RunElevatedCommand"/>), started through UAC.
/// </summary>
public sealed class WindowsServiceInstaller : IServiceInstaller
{
    public const string InstallArgument = "--install-service";
    public const string UninstallArgument = "--uninstall-service";

    private const string ServiceName = "ApexClock";
    private const string ServiceDisplayName = "Apex Clock";
    private const string ServiceDescription = "Tracks the Apex Legends ranked map rotation and notifies the Apex Clock app on every change.";
    private const string ServiceExe = "ApexClock.Service.exe";
    private const int ErrorCancelled = 1223;

    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(30);

    private static string BundledDirectory => Path.Combine(AppContext.BaseDirectory, "Service");

    private static string InstallDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ApexClock", "Service");

    public ServiceInstallState GetState()
    {
        using ServiceController? controller = Find();
        if (controller is null)
            return ServiceInstallState.NotInstalled;

        return controller.Status switch
        {
            ServiceControllerStatus.Running => ServiceInstallState.Running,
            ServiceControllerStatus.StartPending or ServiceControllerStatus.ContinuePending => ServiceInstallState.Starting,
            ServiceControllerStatus.StopPending or ServiceControllerStatus.PausePending => ServiceInstallState.Stopping,
            _ => ServiceInstallState.Stopped,
        };
    }

    public Task<string?> InstallAsync()
    {
        if (!File.Exists(Path.Combine(BundledDirectory, ServiceExe)))
            return Task.FromResult<string?>($"Service files are missing: {Path.Combine(BundledDirectory, ServiceExe)}");
        return RunElevatedAsync(InstallArgument);
    }

    public Task<string?> UninstallAsync() => RunElevatedAsync(UninstallArgument);

    /// <summary>Entry point of the elevated process. Writes the error (if any) to <paramref name="resultPath"/>.</summary>
    /// <returns>Process exit code: 0 on success.</returns>
    public static int RunElevatedCommand(string command, string resultPath)
    {
        try
        {
            if (command == InstallArgument)
                Install();
            else if (command == UninstallArgument)
                Uninstall();
            else
                throw new ArgumentException($"Unknown command {command}.");
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

    private static async Task<string?> RunElevatedAsync(string command)
    {
        string resultPath = Path.Combine(Path.GetTempPath(), $"ApexClock-setup-{Guid.NewGuid():N}.txt");
        ProcessStartInfo info = new ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute = true,
            Verb = "runas",
            Arguments = $"{command} \"{resultPath}\"",
        };

        try
        {
            using Process? process = Process.Start(info);
            if (process is null)
                return "Could not start the installer.";
            await process.WaitForExitAsync();

            string? error = File.Exists(resultPath) ? await File.ReadAllTextAsync(resultPath) : null;
            if (process.ExitCode == 0)
                return null;
            return string.IsNullOrWhiteSpace(error) ? $"Setup failed (exit code {process.ExitCode})." : error;
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

    private static void Install()
    {
        Stop();
        CopyDirectory(BundledDirectory, InstallDirectory);

        string binaryPath = $"\"{Path.Combine(InstallDirectory, ServiceExe)}\"";
        using (ServiceController? existing = Find())
        {
            if (existing is null)
            {
                RunSc("create", ServiceName, "binPath=", binaryPath, "start=", "auto", "DisplayName=", ServiceDisplayName);
                RunSc("description", ServiceName, ServiceDescription);
                RunSc("failure", ServiceName, "reset=", "86400", "actions=", "restart/60000/restart/60000/restart/60000");
            }
            else
            {
                RunSc("config", ServiceName, "binPath=", binaryPath, "start=", "auto");
            }
        }

        using ServiceController controller = new ServiceController(ServiceName);
        controller.Start();
        controller.WaitForStatus(ServiceControllerStatus.Running, StatusTimeout);
    }

    private static void Uninstall()
    {
        Stop();
        using (ServiceController? existing = Find())
        {
            if (existing is not null)
                RunSc("delete", ServiceName);
        }

        // The schedule in %ProgramData%\ApexClock is kept for a later reinstall.
        Retry(() =>
        {
            if (Directory.Exists(InstallDirectory))
                Directory.Delete(InstallDirectory, recursive: true);
        });
    }

    private static void Stop()
    {
        using ServiceController? controller = Find();
        if (controller is null || controller.Status == ServiceControllerStatus.Stopped)
            return;

        if (controller.Status != ServiceControllerStatus.StopPending)
            controller.Stop();
        controller.WaitForStatus(ServiceControllerStatus.Stopped, StatusTimeout);
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

    private static void CopyDirectory(string source, string target)
    {
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            // The stopped service process can hold its files for a moment.
            Retry(() => File.Copy(file, destination, overwrite: true));
        }
    }

    private static void Retry(Action action)
    {
        const int attempts = 10;
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                action();
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
