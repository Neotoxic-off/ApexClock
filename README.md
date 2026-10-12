# ApexClock

Tracks the Apex Legends ranked map rotation and shows a Windows notification every time the map changes.

## Architecture

| Project | Role |
|---|---|
| `ApexClock.Service` | Windows service (`ApexClock`). Computes the live map, stores the rotation in `%ProgramData%\ApexClock\schedule.json` and pushes the state to clients over the `ApexClock.Rotation` named pipe whenever it changes. |
| `Apex Clock` (WPF) | Tray app (WPF-UI Fluent design, MVVM with CommunityToolkit.Mvvm). Shows the live map, countdown and upcoming slots, notifies on map change, and edits the rotation. |
| `ApexClock.Core` | Rotation model and pipe protocol shared by both. |

A Windows service runs in session 0 and cannot show notifications itself: the WPF app (user session) displays the notifications the service sends.

## Rotation

The rotation is a list of maps with their duration, played in order and repeated from a start date.
To sync it with the game: under **Sync with game**, pick the live map, enter the time left shown in game, turn the toggle on and **Save**.

## Install

Build or publish the `Apex Clock` app: the service is published alongside it, in a `Service` folder.
Then, in the app, under **Windows service**:

- **Install** (or **Reinstall** to update) copies the service to `%ProgramFiles%\ApexClock\Service`, registers it as auto-start (restarts on failure) and starts it.
- **Uninstall** stops and removes it. The rotation in `%ProgramData%\ApexClock` is kept.

Both ask for administrator rights (UAC): the app runs an elevated copy of itself with `--install-service` / `--uninstall-service`.

Turn on **Start with Windows** so the app starts minimized to the tray and keeps notifying.

To develop without installing the service: `dotnet run --project "Apex Clock/ApexClock.Service"`.

## Map artwork

`Apex Clock/Assets/Maps/*.jpg` are Apex Legends map key arts (© Electronic Arts / Respawn Entertainment), resized from the images served by apexlegendsstatus.com. A map with no matching file (`<lowercase-name-with-dashes>.jpg`, apostrophes removed) falls back to a neutral gradient.

## Icon and notification sound

Generated with Pillow / the Python standard library, re-run to tweak them:

```powershell
python scripts/assets/make_icon.py "Apex Clock/Apex Clock/Assets" [preview.png]   # AppIcon.ico, AppIcon.png, optional size preview
python scripts/assets/make_sound.py "Apex Clock/Apex Clock/Assets/Sounds/rotation.wav"
```

Notifications are native Windows toasts (app logo + map artwork). Unpackaged apps cannot attach a custom sound to a toast, so the toast is silent and the app plays `rotation.wav` itself, only when Windows notifications are enabled.
