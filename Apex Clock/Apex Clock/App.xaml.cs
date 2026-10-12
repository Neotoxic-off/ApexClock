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
/// Composition root. Tray application: receives rotation updates from the ApexClock service and shows notifications.
/// Closing the window only hides it; "Exit" in the tray menu quits.
/// </summary>
public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\ApexClock.Ui";
    private static readonly Color ApexRed = Color.FromRgb(0xE5, 0x3B, 0x3B);

    private Mutex? _singleInstance;
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        bool isFirstInstance;
        _singleInstance = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out isFirstInstance);
        if (!isFirstInstance)
        {
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown();
            return;
        }

        ApplicationAccentColorManager.Apply(ApexRed, ApplicationTheme.Dark);

        _services = ConfigureServices();
        Ioc.Default.ConfigureServices(_services);

        TrayIcon tray = Ioc.Default.GetRequiredService<TrayIcon>();
        tray.OpenRequested += OnOpenRequested;
        tray.ExitRequested += OnExitRequested;

        Ioc.Default.GetRequiredService<IRotationClient>().Start();

        if (!e.Args.Contains(AutoStartService.MinimizedArgument))
            ShowMainWindow();
    }

    private static ServiceProvider ConfigureServices()
    {
        ServiceCollection services = new ServiceCollection();

        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton<TrayIcon>();
        services.AddSingleton<INotificationService>((IServiceProvider provider) => provider.GetRequiredService<TrayIcon>());
        services.AddSingleton<IAutoStartService, AutoStartService>();
        services.AddSingleton<ServiceClient>();
        services.AddSingleton<IRotationClient>((IServiceProvider provider) => provider.GetRequiredService<ServiceClient>());

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

    private void OnExitRequested(object? sender, EventArgs e)
    {
        Ioc.Default.GetRequiredService<Views.MainWindow>().AllowClose = true;
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Disposes the singletons: view model, pipe client, tray icon.
        _services?.Dispose();
        _singleInstance?.ReleaseMutex();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
