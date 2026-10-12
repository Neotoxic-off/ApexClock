using System.IO;
using System.Windows;
using System.Windows.Resources;

namespace Apex_Clock.Services;

/// <summary>Locations of the resources embedded in the app (Assets folder).</summary>
public static class AppAssets
{
    private const string Root = "pack://application:,,,/ApexClock;component/Assets/";

    public static readonly Uri IconUri = new Uri(Root + "AppIcon.ico");
    public static readonly Uri LogoUri = new Uri(Root + "AppIcon.png");
    public static readonly Uri RotationSoundUri = new Uri(Root + "Sounds/rotation.wav");

    /// <summary>Artwork of a map, e.g. "World's Edge" → Assets/Maps/worlds-edge.jpg (may not exist).</summary>
    public static Uri MapArtworkUri(string map) => new Uri($"{Root}Maps/{MapSlug(map)}.jpg");

    public static string MapSlug(string map) =>
        map.Trim().ToLowerInvariant().Replace("'", "").Replace(' ', '-');

    /// <summary>Opens an embedded resource, or returns null when it does not exist.</summary>
    public static Stream? Open(Uri resource)
    {
        try
        {
            StreamResourceInfo? info = Application.GetResourceStream(resource);
            return info?.Stream;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
