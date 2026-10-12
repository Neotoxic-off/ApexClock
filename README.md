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

```powershell
# Elevated PowerShell: publishes the service and installs it as auto-start (re-run to update)
.\scripts\install-service.ps1

# Uninstall
.\scripts\uninstall-service.ps1
```

Then launch the `Apex Clock` app and turn on **Start with Windows** so it starts minimized to the tray.

To develop without installing the service: `dotnet run --project "Apex Clock/ApexClock.Service"`.
