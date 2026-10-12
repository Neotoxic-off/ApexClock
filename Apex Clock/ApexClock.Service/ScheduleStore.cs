using System.Text.Json;
using ApexClock.Core;

namespace ApexClock.Service;

/// <summary>
/// Persists the schedule in %ProgramData%\ApexClock\schedule.json.
/// <see cref="Current"/> is replaced, never mutated, so readers can compare by reference.
/// </summary>
public sealed class ScheduleStore
{
    private static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly ILogger<ScheduleStore> _logger;
    private readonly string _path;
    private readonly Lock _gate = new Lock();
    private RotationSchedule _current;

    public ScheduleStore(ILogger<ScheduleStore> logger)
    {
        _logger = logger;
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ApexClock");
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "schedule.json");
        _current = Load();
    }

    public RotationSchedule Current
    {
        get { lock (_gate) return _current; }
    }

    public void Save(RotationSchedule schedule)
    {
        lock (_gate)
        {
            string temp = _path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(schedule, Json));
            File.Move(temp, _path, overwrite: true);
            _current = schedule;
        }
    }

    private RotationSchedule Load()
    {
        if (File.Exists(_path))
        {
            try
            {
                RotationSchedule? loaded = JsonSerializer.Deserialize<RotationSchedule>(File.ReadAllText(_path), Json)?.Normalized();
                string? error = loaded is null ? "empty file" : loaded.Validate();
                if (error is null)
                    return loaded!;
                _logger.LogWarning("Schedule {Path} is invalid ({Error}); replaced with the default schedule.", _path, error);
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                _logger.LogWarning(ex, "Could not read {Path}; replaced with the default schedule.", _path);
            }
        }

        RotationSchedule schedule = RotationSchedule.CreateDefault();
        Save(schedule);
        return schedule;
    }
}
