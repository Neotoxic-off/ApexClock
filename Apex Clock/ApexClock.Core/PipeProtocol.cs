using System.Text;
using System.Text.Json;

namespace ApexClock.Core;

/// <summary>
/// Service ↔ UI protocol: one JSON <see cref="PipeMessage"/> per line over a duplex named pipe.
/// The service pushes a snapshot on connect and whenever the rotation advances or the schedule changes.
/// </summary>
public static class PipeProtocol
{
    public const string PipeName = "ApexClock.Rotation";

    public static readonly Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public static string Serialize(PipeMessage message) => JsonSerializer.Serialize(message, Json);

    public static PipeMessage? Deserialize(string line) => JsonSerializer.Deserialize<PipeMessage>(line, Json);
}

public sealed class PipeMessage
{
    /// <summary>Service → UI: current schedule and rotation state.</summary>
    public const string SnapshotType = "snapshot";

    /// <summary>UI → service: replace the schedule.</summary>
    public const string SetScheduleType = "setSchedule";

    /// <summary>Service → UI: a request was rejected.</summary>
    public const string ErrorType = "error";

    public string Type { get; set; } = "";
    public RotationSchedule? Schedule { get; set; }
    public RotationState? State { get; set; }
    public string? Error { get; set; }

    public static PipeMessage Snapshot(RotationSchedule schedule, DateTimeOffset now) => new PipeMessage
    {
        Type = SnapshotType,
        Schedule = schedule,
        State = schedule.GetState(now),
    };

    public static PipeMessage SetSchedule(RotationSchedule schedule) => new PipeMessage { Type = SetScheduleType, Schedule = schedule };

    public static PipeMessage Failure(string error) => new PipeMessage { Type = ErrorType, Error = error };
}
