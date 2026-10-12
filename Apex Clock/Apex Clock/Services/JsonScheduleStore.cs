using System.IO;
using System.Text.Json;
using ApexClock.Core;
using Apex_Clock.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace Apex_Clock.Services;

/// <summary>
/// Stores the schedule in %LocalAppData%\ApexClock\schedule.json. On first run, the rotation saved by the former
/// Windows service (%ProgramData%\ApexClock\schedule.json) is imported, otherwise the default one is used.
/// </summary>
public sealed class JsonScheduleStore : IScheduleStore
{
    private static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly IMessenger _messenger;
    private readonly string _path;

    public JsonScheduleStore(IMessenger messenger)
        : this(messenger, AppPaths.SchedulePath, AppPaths.LegacySchedulePath)
    {
    }

    public JsonScheduleStore(IMessenger messenger, string path, string legacyPath)
    {
        _messenger = messenger;
        _path = path;

        RotationSchedule? stored = Load(path);
        if (stored is not null)
        {
            Current = stored;
            return;
        }

        // Written right away so the import survives the removal of the former service's data.
        Current = Load(legacyPath) ?? RotationSchedule.CreateDefault();
        Write(Current);
    }

    public RotationSchedule Current { get; private set; }

    public string? Save(RotationSchedule schedule)
    {
        RotationSchedule normalized = schedule.Normalized();
        string? error = normalized.Validate();
        if (error is not null)
            return error;

        string? writeError = Write(normalized);
        if (writeError is not null)
            return writeError;

        Current = normalized;
        _messenger.Send(new ScheduleChangedMessage(normalized));
        return null;
    }

    /// <summary>Atomically replaces the file. Returns an error message, or null on success.</summary>
    private string? Write(RotationSchedule schedule)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            string temp = _path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(schedule, Json));
            File.Move(temp, _path, overwrite: true);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"Could not save the rotation: {ex.Message}";
        }
    }

    /// <summary>The usable schedule stored at <paramref name="path"/>, or null when missing or invalid.</summary>
    private static RotationSchedule? Load(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;
            RotationSchedule? loaded = JsonSerializer.Deserialize<RotationSchedule>(File.ReadAllText(path), Json)?.Normalized();
            return loaded?.Validate() is null ? loaded : null;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
