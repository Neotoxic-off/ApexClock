# ApexClock

Tracks the Apex Legends ranked map rotation and shows a Windows notification every time the map changes.

## Architecture

| Project | Role |
|---|---|
| `Apex Clock` (WPF) | Tray app (WPF-UI Fluent design, MVVM with CommunityToolkit.Mvvm). Computes the live map, keeps a live tray icon, notifies on every map change, and edits the rotation. |
| `ApexClock.Core` | Rotation model: `RotationSchedule` (anchor + durations), `RotationCycle` (a cycle as wall-clock times), `ClockTime`. |
| `ApexClock.Core.Tests`, `ApexClock.App.Tests` | xUnit tests of the rotation model, the schedule store and the rotation editor. Run `dotnet test "Apex Clock/Apex Clock.slnx"`. |

One view model per panel (`LiveRotationViewModel`, `RotationEditorViewModel`, `SettingsViewModel`, `LegacyServiceViewModel`), composed by `MainViewModel`. Background behaviour does not depend on the window: `ClockTicker` publishes a tick every second, `TrayPresenter` keeps the tray icon up to date and `MapChangeNotifier` raises the notifications. The schedule lives in `IScheduleStore`; time comes from an injected `TimeProvider`.

## Rotation

The rotation is a list of maps played back to back, then repeated (e.g. 3 maps × 4h30 = a 13h30 cycle, so the times shift from day to day).
Under **Edit rotation**, enter the local time each map of the current cycle goes live (`19:00`, `19h30`, `1930`…) and when the cycle repeats, then **Save**.
It is stored in `%LocalAppData%\ApexClock\schedule.json`.

## Run

Unzip a release and start `ApexClock.exe` (requires the .NET 10 desktop runtime). Turn on **Start with Windows** so it starts minimized to the tray at sign-in.
Click the tray icon to open the window; launching the app again also brings it up.

Versions before 1.2 installed an `ApexClock` Windows service. It is no longer needed: on first run the app imports its rotation, and an **Old Windows service** card offers to remove it (asks for administrator rights; the app runs an elevated copy of itself with `--remove-legacy-service`).

## Map artwork

`Apex Clock/Assets/Maps/*.jpg` are Apex Legends map key arts (© Electronic Arts / Respawn Entertainment), resized from the images served by apexlegendsstatus.com. A map with no matching file (`<lowercase-name-with-dashes>.jpg`, apostrophes removed) falls back to a neutral gradient.

## Icon and notification sound

Generated with Pillow / the Python standard library, re-run to tweak them:

```powershell
python scripts/assets/make_icon.py "Apex Clock/Apex Clock/Assets" [preview.png]   # AppIcon.ico, AppIcon.png, optional size preview
python scripts/assets/make_sound.py "Apex Clock/Apex Clock/Assets/Sounds/rotation.wav"
```

Notifications are native Windows toasts (app logo + map artwork). Unpackaged apps cannot attach a custom sound to a toast, so the toast is silent and the app plays `rotation.wav` itself, only when Windows notifications are enabled.
