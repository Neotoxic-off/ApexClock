using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Apex_Clock.Services;

/// <summary>Dark Windows 11 look for the tray <see cref="ContextMenuStrip"/>, matching the app's Fluent dark theme.</summary>
public sealed class FluentMenuRenderer : ToolStripRenderer
{
    private static readonly Color Background = Color.FromArgb(0x2C, 0x2C, 0x2C);
    private static readonly Color Hover = Color.FromArgb(0x3D, 0x3D, 0x3D);
    private static readonly Color Text = Color.FromArgb(0xFF, 0xFF, 0xFF);
    private static readonly Color DisabledText = Color.FromArgb(0x9D, 0x9D, 0x9D);
    private static readonly Color Divider = Color.FromArgb(0x40, 0x40, 0x40);

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWCP_ROUND = 2;

    /// <summary>Applies the renderer, font and spacing, and rounds the popup corners once its window exists.</summary>
    public static void Apply(ContextMenuStrip menu)
    {
        menu.Renderer = new FluentMenuRenderer();
        menu.ShowImageMargin = false;
        menu.Font = new Font("Segoe UI", 9.5f);
        menu.Padding = new Padding(4);
        menu.HandleCreated += (object? sender, EventArgs args) => RoundCorners(menu.Handle);
        foreach (ToolStripItem item in menu.Items)
        {
            if (item is ToolStripMenuItem)
            {
                item.Padding = new Padding(10, 6, 24, 6);
                item.Margin = new Padding(0, 1, 0, 1);
            }
        }
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) =>
        e.Graphics.Clear(Background);

    // DWM draws the Windows 11 border around the rounded popup.
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected || !e.Item.Enabled)
            return;

        Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
        bounds.Inflate(-2, 0);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using GraphicsPath path = RoundedRect(bounds, 4);
        using SolidBrush brush = new SolidBrush(Hover);
        e.Graphics.FillPath(brush, path);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Text : DisabledText;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        int y = e.Item.Height / 2;
        using Pen pen = new Pen(Divider);
        e.Graphics.DrawLine(pen, 4, y, e.Item.Width - 4, y);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int d = radius * 2;
        GraphicsPath path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static void RoundCorners(IntPtr handle)
    {
        // Both attributes are Windows 11 only; older systems ignore the call and keep square corners.
        int corner = DWMWCP_ROUND;
        DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
        int border = ColorTranslator.ToWin32(Divider);
        DwmSetWindowAttribute(handle, DWMWA_BORDER_COLOR, ref border, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
