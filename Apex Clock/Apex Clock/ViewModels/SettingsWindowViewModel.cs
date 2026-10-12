namespace Apex_Clock.ViewModels;

/// <summary>The settings window: rotation editor, preferences and the legacy service cleanup.</summary>
public sealed class SettingsWindowViewModel(
    RotationEditorViewModel editor,
    SettingsViewModel settings,
    LegacyServiceViewModel legacyService)
{
    public RotationEditorViewModel Editor { get; } = editor;

    public SettingsViewModel Settings { get; } = settings;

    public LegacyServiceViewModel LegacyService { get; } = legacyService;
}
