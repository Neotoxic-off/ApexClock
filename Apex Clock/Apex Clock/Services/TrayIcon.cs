using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Apex_Clock.Services;

/// <summary>Notification area icon with Open/Settings/Exit menu, the live map drawn in the icon and detailed in the tooltip.</summary>
public sealed class TrayIcon : ITrayService, IDisposable
{
    // NotifyIcon.Text throws past 127 characters.
    private const int MaxTooltipLength = 127;

    private readonly NotifyIcon _icon;
    private readonly Icon _appIcon;
    private Icon? _liveIcon;
    private string? _liveKey;

    public TrayIcon()
    {
        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (object? sender, EventArgs args) => OpenRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("Settings", null, (object? sender, EventArgs args) => SettingsRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (object? sender, EventArgs args) => ExitRequested?.Invoke(this, EventArgs.Empty));
        FluentMenuRenderer.Apply(menu);

        _appIcon = LoadIcon();
        _icon = new NotifyIcon
        {
            Icon = _appIcon,
            Text = "Apex Clock",
            ContextMenuStrip = menu,
            Visible = true,
        };
        // A single left click opens the window; the right click shows the menu.
        _icon.MouseClick += (object? sender, MouseEventArgs args) =>
        {
            if (args.Button == MouseButtons.Left)
                OpenRequested?.Invoke(this, EventArgs.Empty);
        };
    }

    public event EventHandler? OpenRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler? ExitRequested;

    public void SetStatus(string text) =>
        _icon.Text = text.Length > MaxTooltipLength ? text[..MaxTooltipLength] : text;

    public void SetLiveMap(string? map, double progress)
    {
        if (map is null)
        {
            if (_liveKey is not null)
                SwapIcon(null, null);
            return;
        }

        // Redraw only when the initials or the ring step change, not on every clock tick.
        string initials = LiveTrayIcon.Initials(map);
        int step = (int)Math.Round(Math.Clamp(progress, 0, 1) * LiveTrayIcon.ProgressSteps);
        string key = $"{initials}:{step}";
        if (key != _liveKey)
            SwapIcon(LiveTrayIcon.Render(initials, step, SystemInformation.SmallIconSize.Width), key);
    }

    private void SwapIcon(Icon? live, string? key)
    {
        Icon? previous = _liveIcon;
        _liveIcon = live;
        _liveKey = key;
        _icon.Icon = live ?? _appIcon;
        previous?.Dispose();
    }

    private static Icon LoadIcon()
    {
        using Stream? stream = AppAssets.Open(AppAssets.IconUri);
        return stream is null ? SystemIcons.Application : new Icon(stream, SystemInformation.SmallIconSize);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _liveIcon?.Dispose();
        _appIcon.Dispose();
    }
}
