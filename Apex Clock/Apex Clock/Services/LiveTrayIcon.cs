using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace Apex_Clock.Services;

/// <summary>Draws the live tray icon: map initials inside a progress ring, in the app icon's glass style.</summary>
public static class LiveTrayIcon
{
    /// <summary>Ring resolution: the icon is redrawn at most this many times per map.</summary>
    public const int ProgressSteps = 60;

    // Drawn larger then downscaled so the 16–24 px result stays smooth.
    private const int Supersample = 4;

    private static readonly Color Disc = Color.FromArgb(0x1E, 0x22, 0x28);
    private static readonly Color Track = Color.FromArgb(0x50, 0x50, 0x56);
    private static readonly Color Accent = Color.FromArgb(0x4C, 0xC2, 0xFF);

    /// <summary>"Olympus" → "OL", "World's Edge" → "WE", "E-District" → "ED".</summary>
    public static string Initials(string map)
    {
        string[] words = map.Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2)
            return $"{char.ToUpperInvariant(words[0][0])}{char.ToUpperInvariant(words[1][0])}";
        string word = words.Length == 1 ? words[0] : "?";
        return word.Length >= 2 ? word[..2].ToUpperInvariant() : word.ToUpperInvariant();
    }

    /// <summary>Renders the icon; the caller owns it and must dispose it.</summary>
    public static Icon Render(string initials, int step, int size)
    {
        int big = size * Supersample;
        using Bitmap canvas = new Bitmap(big, big);
        using (Graphics g = Graphics.FromImage(canvas))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            float stroke = big * 0.12f;
            RectangleF ring = new RectangleF(stroke / 2, stroke / 2, big - stroke, big - stroke);
            using (SolidBrush disc = new SolidBrush(Disc))
                g.FillEllipse(disc, ring);
            using (Pen track = new Pen(Track, stroke))
                g.DrawEllipse(track, ring);
            if (step > 0)
            {
                using Pen arc = new Pen(Accent, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawArc(arc, ring, -90, 360f * Math.Min(step, ProgressSteps) / ProgressSteps);
            }

            using Font font = new Font("Segoe UI", big * 0.46f, FontStyle.Bold, GraphicsUnit.Pixel);
            using StringFormat center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(initials, font, Brushes.White, new RectangleF(-big * 0.1f, big * 0.02f, big * 1.2f, big), center);
        }

        using Bitmap icon = new Bitmap(size, size);
        using (Graphics g = Graphics.FromImage(icon))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(canvas, 0, 0, size, size);
        }

        // GetHicon hands out a GDI handle the Icon does not own: clone it, then free the handle.
        IntPtr handle = icon.GetHicon();
        try
        {
            using Icon borrowed = Icon.FromHandle(handle);
            return (Icon)borrowed.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
