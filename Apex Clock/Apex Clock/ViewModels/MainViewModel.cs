namespace Apex_Clock.ViewModels;

/// <summary>The main window: one view model per panel.</summary>
public sealed class MainViewModel(
    LiveRotationViewModel live,
    RotationEditorViewModel editor,
    SettingsViewModel settings,
    LegacyServiceViewModel legacyService)
{
    public LiveRotationViewModel Live { get; } = live;

    public RotationEditorViewModel Editor { get; } = editor;

    public SettingsViewModel Settings { get; } = settings;

    public LegacyServiceViewModel LegacyService { get; } = legacyService;
}
