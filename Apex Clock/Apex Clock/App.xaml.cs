using System.Windows;
using System.Windows.Media;
using Apex_Clock.Services;
using Apex_Clock.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Appearance;

namespace Apex_Clock;

/// <summary>
/// Composition root. Tray application: computes the ranked rotation, keeps the tray icon live and notifies on
/// every map change. Closing the window only hides it; "Exit" in the tray menu quits.
/// </summary>
public partial class App : Application
{
    /// <summary>Ice blue of the app icon.</summary>
    private static readonly Color Accent = Color.FromRgb(0x4C, 0xC2, 0xFF);

    private SingleInstance? _singleInstance;
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Elevated copy started by LegacyServiceRemover: remove the old service and quit, no UI.
        if (e.Args.Length == 2 && e.Args[0] == LegacyServiceRemover.RemoveArgument)
        {
            Shutdown(LegacyServiceRemover.RunElevated(e.Args[1]));
            return;
        }

        _singleInstance = SingleInstance.TryAcquire();
        if (_singleInstance is null)
        {
            Shutdown();
            return;
        }

        ApplicationAccentColorManager.Apply(Accent, ApplicationTheme.Dark);

        _services = ConfigureServices();
        Ioc.Default.ConfigureServices(_services);

        // Each of these arrives on a background thread.
        _singleInstance.ActivationRequested += OnShowRequestedFromBackground;
        Ioc.Default.GetRequiredService<ToastNotificationService>().Activated += OnShowRequestedFromBackground;

        TrayIcon tray = Ioc.Default.GetRequiredService<TrayIcon>();
        tray.OpenRequested += OnOpenRequested;
        tray.ExitRequested += OnExitRequested;

        // Background behaviour runs without the window: notifications and the live tray icon, driven by the clock.
        Ioc.Default.GetRequiredService<MapChangeNotifier>();
        Ioc.Default.GetRequiredService<TrayPresenter>();
        Ioc.Default.GetRequiredService<ClockTicker>().Start();

        if (!e.Args.Contains(AutoStartService.MinimizedArgument))
            ShowMainWindow();
    }

    private static ServiceProvider ConfigureServices()
    {
        ServiceCollection services = new ServiceCollection();

        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ClockTicker>();
        services.AddSingleton<IScheduleStore, JsonScheduleStore>();
        services.AddSingleton<TrayIcon>();
        services.AddSingleton<ITrayService>((IServiceProvider provider) => provider.GetRequiredService<TrayIcon>());
        services.AddSingleton<ToastNotificationService>();
        services.AddSingleton<INotificationService>((IServiceProvider provider) => provider.GetRequiredService<ToastNotificationService>());
        services.AddSingleton<IAutoStartService, AutoStartService>();
        services.AddSingleton<ILegacyServiceRemover, LegacyServiceRemover>();
        services.AddSingleton<MapChangeNotifier>();
        services.AddSingleton<TrayPresenter>();

        services.AddSingleton<LiveRotationViewModel>();
        services.AddSingleton<RotationEditorViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<LegacyServiceViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<Views.MainWindow>();

        return services.BuildServiceProvider();
    }

    private void ShowMainWindow()
    {
        Views.MainWindow window = Ioc.Default.GetRequiredService<Views.MainWindow>();
        window.Show();
        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;
        window.Activate();
    }

    private void OnOpenRequested(object? sender, EventArgs e) => ShowMainWindow();

    private void OnShowRequestedFromBackground(object? sender, EventArgs e) => Dispatcher.InvokeAsync(ShowMainWindow);

    private void OnExitRequested(object? sender, EventArgs e)
    {
        Ioc.Default.GetRequiredService<Views.MainWindow>().AllowClose = true;
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Disposes the singletons: view models, clock, tray icon, toast service.
        _services?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
