using System.IO;
using ApexClock.Core;
using Apex_Clock.Messages;
using Apex_Clock.Services;
using CommunityToolkit.Mvvm.Messaging;

namespace ApexClock.App.Tests;

public sealed class JsonScheduleStoreTests : IDisposable
{
    private const string LegacyJson = """
        { "anchor": "2026-10-12T08:00:00+00:00",
          "entries": [ { "map": "Storm Point", "durationMinutes": 120 }, { "map": "E-District", "durationMinutes": 60 } ] }
        """;

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"ApexClockTests-{Guid.NewGuid():N}");
    private readonly IMessenger _messenger = new StrongReferenceMessenger();

    private string StorePath => Path.Combine(_directory, "local", "schedule.json");

    private string LegacyPath => Path.Combine(_directory, "legacy", "schedule.json");

    private JsonScheduleStore CreateStore() => new JsonScheduleStore(_messenger, StorePath, LegacyPath);

    private void WriteLegacy(string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LegacyPath)!);
        File.WriteAllText(LegacyPath, json);
    }

    [Fact]
    public void Starts_with_the_default_rotation_and_writes_it()
    {
        JsonScheduleStore store = CreateStore();

        Assert.Equal(RotationSchedule.CreateDefault().Anchor, store.Current.Anchor);
        Assert.True(File.Exists(StorePath));
    }

    [Fact]
    public void Imports_the_former_service_rotation_and_keeps_it_once_that_is_deleted()
    {
        WriteLegacy(LegacyJson);
        Assert.Equal("Storm Point", CreateStore().Current.Entries[0].Map);

        File.Delete(LegacyPath);

        Assert.Equal("Storm Point", CreateStore().Current.Entries[0].Map);
    }

    [Fact]
    public void Ignores_an_unusable_legacy_file()
    {
        WriteLegacy("""{ "timeZoneId": "Romance Standard Time", "entries": [ { "map": "Olympus", "startMinute": 600 } ] }""");

        Assert.Equal(RotationSchedule.CreateDefault().Entries.Count, CreateStore().Current.Entries.Count);
    }

    [Fact]
    public void Save_persists_announces_and_normalizes()
    {
        JsonScheduleStore store = CreateStore();
        RotationSchedule? announced = null;
        _messenger.Register<ScheduleChangedMessage>(this, (object recipient, ScheduleChangedMessage message) => announced = message.Value);

        string? error = store.Save(new RotationSchedule
        {
            Anchor = RotationSchedule.CreateDefault().Anchor,
            Entries = [new RotationEntry { Map = "  Olympus ", DurationMinutes = 90 }],
        });

        Assert.Null(error);
        Assert.Equal("Olympus", announced!.Entries[0].Map);
        Assert.Equal("Olympus", CreateStore().Current.Entries[0].Map);
    }

    [Fact]
    public void Save_rejects_an_invalid_schedule()
    {
        JsonScheduleStore store = CreateStore();

        string? error = store.Save(new RotationSchedule { Anchor = RotationSchedule.CreateDefault().Anchor });

        Assert.NotNull(error);
        Assert.Equal(3, store.Current.Entries.Count);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
