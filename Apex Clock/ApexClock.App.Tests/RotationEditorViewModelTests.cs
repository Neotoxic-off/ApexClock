using ApexClock.Core;
using Apex_Clock.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace ApexClock.App.Tests;

public sealed class RotationEditorViewModelTests
{
    private readonly FixedTimeProvider _time = new FixedTimeProvider(new DateTimeOffset(2026, 10, 12, 12, 40, 0, TimeSpan.FromHours(2)));
    private readonly IMessenger _messenger = new StrongReferenceMessenger();
    private readonly FakeScheduleStore _store;

    public RotationEditorViewModelTests() => _store = new FakeScheduleStore(_messenger);

    private RotationEditorViewModel CreateEditor() => new RotationEditorViewModel(_store, _time, _messenger);

    private static RotationSchedule TwoMaps() => new RotationSchedule
    {
        Anchor = RotationSchedule.CreateDefault().Anchor,
        Entries = [new RotationEntry { Map = "Kings Canyon", DurationMinutes = 360 }, new RotationEntry { Map = "Olympus", DurationMinutes = 360 }],
    };

    [Fact]
    public void Opens_clean_with_the_running_cycle()
    {
        RotationEditorViewModel editor = CreateEditor();

        Assert.False(editor.IsDirty);
        Assert.False(editor.SaveCommand.CanExecute(null));
        Assert.Equal(["Olympus", "Broken Moon", "World's Edge"], editor.Entries.Select((EntryViewModel e) => e.Map));
        Assert.Equal(["10:00", "14:30", "19:00"], editor.Entries.Select((EntryViewModel e) => e.StartText));
        Assert.Equal(["4h30", "4h30", "4h30"], editor.Entries.Select((EntryViewModel e) => e.Length));
        Assert.Equal("23:30", editor.CycleEndText);
    }

    [Fact]
    public void A_clean_editor_follows_schedule_changes()
    {
        RotationEditorViewModel editor = CreateEditor();

        _store.Replace(TwoMaps());

        Assert.Equal(["Kings Canyon", "Olympus"], editor.Entries.Select((EntryViewModel e) => e.Map));
        Assert.False(editor.IsDirty);
    }

    [Fact]
    public void A_dirty_editor_keeps_its_rows_on_schedule_changes()
    {
        RotationEditorViewModel editor = CreateEditor();
        editor.Entries[0].Map = "Storm Point";

        _store.Replace(TwoMaps());

        Assert.Equal("Storm Point", editor.Entries[0].Map);
        Assert.True(editor.IsDirty);
    }

    [Fact]
    public void Editing_a_time_makes_it_dirty_and_undoing_it_makes_it_clean()
    {
        RotationEditorViewModel editor = CreateEditor();

        editor.Entries[1].StartText = "15h";
        Assert.True(editor.IsDirty);
        Assert.Equal(["5h", "4h", "4h30"], editor.Entries.Select((EntryViewModel e) => e.Length));

        editor.Entries[1].StartText = "14:30";
        Assert.False(editor.IsDirty);
    }

    [Fact]
    public void An_invalid_time_blanks_the_lengths_and_blocks_saving()
    {
        RotationEditorViewModel editor = CreateEditor();

        editor.Entries[2].StartText = "25h";
        editor.SaveCommand.Execute(null);

        Assert.False(editor.Entries[2].IsStartValid);
        Assert.All(editor.Entries, (EntryViewModel e) => Assert.Equal("", e.Length));
        Assert.Empty(_store.Saved);
        Assert.StartsWith("Row 3", editor.StatusMessage);
    }

    [Fact]
    public void Save_stores_the_cycle_as_a_schedule_and_cleans_the_editor()
    {
        RotationEditorViewModel editor = CreateEditor();
        editor.CycleEndText = "00:00";

        editor.SaveCommand.Execute(null);

        RotationSchedule saved = Assert.Single(_store.Saved);
        Assert.Equal(RotationSchedule.CreateDefault().Anchor, saved.Anchor);
        Assert.Equal([270, 270, 300], saved.Entries.Select((RotationEntry e) => e.DurationMinutes));
        Assert.Equal("00:00", editor.CycleEndText);
        Assert.False(editor.IsDirty);
        Assert.Equal("Rotation saved", editor.StatusMessage);
    }

    [Fact]
    public void Add_appends_a_map_at_the_cycle_end()
    {
        RotationEditorViewModel editor = CreateEditor();

        editor.AddCommand.Execute(null);

        Assert.Equal("23:30", editor.Entries[3].StartText);
        Assert.Equal("04:00", editor.CycleEndText);
        Assert.True(editor.IsDirty);
    }

    [Fact]
    public void Revert_restores_the_saved_cycle()
    {
        RotationEditorViewModel editor = CreateEditor();
        editor.Entries.RemoveAt(2);

        editor.RevertCommand.Execute(null);

        Assert.Equal(3, editor.Entries.Count);
        Assert.False(editor.IsDirty);
    }

    [Fact]
    public void The_last_row_cannot_be_removed()
    {
        RotationEditorViewModel editor = CreateEditor();
        editor.Entries.RemoveAt(2);
        editor.Entries.RemoveAt(1);

        Assert.False(editor.RemoveCommand.CanExecute(editor.Entries[0]));
    }
}
