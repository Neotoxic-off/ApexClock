using System.Drawing;
using System.Windows.Forms;

namespace Apex_Clock.Services;

/// <summary>Notification area icon; balloon tips are rendered as toasts on Windows 10/11.</summary>
public sealed class TrayIcon : INotificationService, IDisposable
{
    // NotifyIcon.Text throws past 127 characters.
    private const int MaxTooltipLength = 127;

    private readonly NotifyIcon _icon;

    public TrayIcon()
    {
        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (object? sender, EventArgs args) => OpenRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("Exit", null, (object? sender, EventArgs args) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Apex Clock",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.DoubleClick += (object? sender, EventArgs args) => OpenRequested?.Invoke(this, EventArgs.Empty);
        _icon.BalloonTipClicked += (object? sender, EventArgs args) => OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? OpenRequested;
    public event EventHandler? ExitRequested;

    public void ShowNotification(string title, string message) => _icon.ShowBalloonTip(10_000, title, message, ToolTipIcon.Info);

    public void SetStatus(string text) =>
        _icon.Text = text.Length > MaxTooltipLength ? text[..MaxTooltipLength] : text;

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
