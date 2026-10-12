using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Apex_Clock.Services;

/// <summary>Notification area icon with Open/Exit menu and the live map as tooltip.</summary>
public sealed class TrayIcon : ITrayService, IDisposable
{
    // NotifyIcon.Text throws past 127 characters.
    private const int MaxTooltipLength = 127;

    private readonly NotifyIcon _icon;

    public TrayIcon()
    {
        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (object? sender, EventArgs args) => OpenRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (object? sender, EventArgs args) => ExitRequested?.Invoke(this, EventArgs.Empty));
        FluentMenuRenderer.Apply(menu);

        _icon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Apex Clock",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.DoubleClick += (object? sender, EventArgs args) => OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? OpenRequested;
    public event EventHandler? ExitRequested;

    public void SetStatus(string text) =>
        _icon.Text = text.Length > MaxTooltipLength ? text[..MaxTooltipLength] : text;

    private static Icon LoadIcon()
    {
        using Stream? stream = AppAssets.Open(AppAssets.IconUri);
        return stream is null ? SystemIcons.Application : new Icon(stream, SystemInformation.SmallIconSize);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
