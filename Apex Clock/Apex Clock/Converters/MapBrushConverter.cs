using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Apex_Clock.Services;

namespace Apex_Clock.Converters;

/// <summary>
/// Map name → map artwork brush from Assets/Maps (e.g. "World's Edge" → worlds-edge.jpg).
/// Maps without artwork get a neutral gradient.
/// </summary>
public sealed class MapBrushConverter : IValueConverter
{
    private static readonly Dictionary<string, Brush?> ArtworkCache = new Dictionary<string, Brush?>(StringComparer.OrdinalIgnoreCase);

    private static readonly Brush Fallback = CreateFallback();

    /// <summary>Horizontal alignment of the artwork when it is cropped to fill the target.</summary>
    public AlignmentX AlignmentX { get; set; } = AlignmentX.Center;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string map && !string.IsNullOrWhiteSpace(map) ? GetArtwork(map.Trim(), AlignmentX) ?? Fallback : Fallback;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Brush? GetArtwork(string map, AlignmentX alignment)
    {
        string key = $"{map}|{alignment}";
        if (ArtworkCache.TryGetValue(key, out Brush? cached))
            return cached;

        Brush? brush = null;
        Uri uri = AppAssets.MapArtworkUri(map);
        try
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = uri;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();

            ImageBrush imageBrush = new ImageBrush(image) { Stretch = Stretch.UniformToFill, AlignmentX = alignment };
            imageBrush.Freeze();
            brush = imageBrush;
        }
        catch (IOException)
        {
            // No artwork for this map name.
        }

        ArtworkCache[key] = brush;
        return brush;
    }

    private static Brush CreateFallback()
    {
        LinearGradientBrush brush = new LinearGradientBrush(Color.FromRgb(0x4A, 0x52, 0x60), Color.FromRgb(0x1E, 0x22, 0x2A), new Point(0, 0), new Point(1, 1));
        brush.Freeze();
        return brush;
    }
}
