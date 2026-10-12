using Apex_Clock.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Apex_Clock.ViewModels;

/// <summary>Install / uninstall of the Windows service, with its live status.</summary>
public sealed partial class ServiceSetupViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(3);

    private readonly IServiceInstaller _installer;
    private readonly CancellationTokenSource _refreshCts = new CancellationTokenSource();

    /// <remarks>Must be created on the UI thread: the refresh loop resumes on the captured context.</remarks>
    public ServiceSetupViewModel(IServiceInstaller installer)
    {
        _installer = installer;
        Message = "";
        Refresh();
        _ = RunRefreshAsync(_refreshCts.Token);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInstalled))]
    [NotifyPropertyChangedFor(nameof(StateText))]
    [NotifyPropertyChangedFor(nameof(InstallLabel))]
    [NotifyCanExecuteChangedFor(nameof(UninstallCommand))]
    public partial ServiceInstallState State { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    [NotifyCanExecuteChangedFor(nameof(UninstallCommand))]
    public partial bool IsBusy { get; private set; }

    /// <summary>Outcome of the last install/uninstall.</summary>
    [ObservableProperty]
    public partial string Message { get; private set; }

    public bool IsInstalled => State != ServiceInstallState.NotInstalled;

    public string InstallLabel => IsInstalled ? "Reinstall" : "Install";

    public string StateText => State switch
    {
        ServiceInstallState.NotInstalled => "Not installed",
        ServiceInstallState.Stopped => "Installed · stopped",
        ServiceInstallState.Starting => "Starting…",
        ServiceInstallState.Running => "Installed · running",
        ServiceInstallState.Stopping => "Stopping…",
        _ => "",
    };

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private Task InstallAsync() => RunAsync(_installer.InstallAsync, "Service installed and running.");

    private bool CanInstall() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanUninstall))]
    private Task UninstallAsync() => RunAsync(_installer.UninstallAsync, "Service uninstalled. Your rotation is kept for a reinstall.");

    private bool CanUninstall() => !IsBusy && IsInstalled;

    private async Task RunAsync(Func<Task<string?>> operation, string successMessage)
    {
        IsBusy = true;
        Message = "Waiting for administrator approval…";
        try
        {
            string? error = await operation();
            Message = error ?? successMessage;
        }
        finally
        {
            IsBusy = false;
            Refresh();
        }
    }

    private async Task RunRefreshAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(RefreshInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
                Refresh();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Refresh() => State = _installer.GetState();

    public void Dispose() => _refreshCts.Cancel();
}
