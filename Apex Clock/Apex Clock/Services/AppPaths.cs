using System.IO;

namespace Apex_Clock.Services;

/// <summary>Where the app keeps its files.</summary>
public static class AppPaths
{
    /// <summary>%LocalAppData%\ApexClock.</summary>
    public static string DataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ApexClock");

    public static string SchedulePath => Path.Combine(DataDirectory, "schedule.json");

    /// <summary>Schedule of the former Windows service, imported once.</summary>
    public static string LegacySchedulePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ApexClock", "schedule.json");
}
