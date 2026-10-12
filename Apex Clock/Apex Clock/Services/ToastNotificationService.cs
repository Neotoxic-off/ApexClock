using System.IO;
using System.Media;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

namespace Apex_Clock.Services;

/// <summary>
/// Native Windows toasts with the app logo and the map artwork as hero image.
/// Unpackaged apps cannot give toasts a custom sound file, so the toast is silent and the app plays its own chime.
/// </summary>
public sealed class ToastNotificationService : INotificationService, IDisposable
{
    private readonly HashSet<string> _extracted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly string _imageDirectory;
    private readonly string? _logoPath;
    private readonly SoundPlayer? _chime;

    public ToastNotificationService()
    {
        // Toast images must be plain files, so embedded resources are copied out once.
        _imageDirectory = Path.Combine(AppPaths.DataDirectory, "Toast");
        Directory.CreateDirectory(_imageDirectory);
        _logoPath = Extract(AppAssets.LogoUri, "logo.png");

        Stream? sound = AppAssets.Open(AppAssets.RotationSoundUri);
        if (sound is not null)
        {
            _chime = new SoundPlayer(sound);
            _chime.Load();
        }

        ToastNotificationManagerCompat.OnActivated += OnToastActivated;
    }

    /// <summary>The user clicked a toast. Raised on a background thread.</summary>
    public event EventHandler? Activated;

    public void ShowMapChange(string map, string title, string message)
    {
        ToastContentBuilder builder = new ToastContentBuilder()
            .AddText(title)
            .AddText(message)
            .AddAudio(new ToastAudio { Silent = true });

        if (_logoPath is not null)
            builder.AddAppLogoOverride(new Uri(_logoPath));

        string? artwork = Extract(AppAssets.MapArtworkUri(map), $"{AppAssets.MapSlug(map)}.jpg");
        if (artwork is not null)
            builder.AddHeroImage(new Uri(artwork));

        builder.Show();

        // Stay quiet when the user turned notifications off.
        if (ToastNotificationManagerCompat.CreateToastNotifier().Setting == NotificationSetting.Enabled)
            _chime?.Play();
    }

    /// <summary>Copies a resource to disk once per run, so updated assets replace stale copies.</summary>
    private string? Extract(Uri resource, string fileName)
    {
        string path = Path.Combine(_imageDirectory, fileName);
        if (_extracted.Contains(fileName))
            return path;

        using Stream? source = AppAssets.Open(resource);
        if (source is null)
            return null;

        using (FileStream target = File.Create(path))
            source.CopyTo(target);
        _extracted.Add(fileName);
        return path;
    }

    private void OnToastActivated(ToastNotificationActivatedEventArgsCompat args) => Activated?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        ToastNotificationManagerCompat.OnActivated -= OnToastActivated;
        _chime?.Dispose();
    }
}
