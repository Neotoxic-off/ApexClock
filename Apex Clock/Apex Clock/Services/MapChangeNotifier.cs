using ApexClock.Core;
using Apex_Clock.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace Apex_Clock.Services;

/// <summary>Raises a notification whenever the live map changes.</summary>
public sealed class MapChangeNotifier : IRecipient<ClockTickMessage>, IDisposable
{
    private readonly IMessenger _messenger;
    private readonly IScheduleStore _store;
    private readonly INotificationService _notifications;
    private string? _lastMap;

    public MapChangeNotifier(IMessenger messenger, IScheduleStore store, INotificationService notifications)
    {
        _messenger = messenger;
        _store = store;
        _notifications = notifications;
        _messenger.RegisterAll(this);
    }

    public void Receive(ClockTickMessage message)
    {
        RotationState? state = _store.Current.GetState(message.Value);
        if (state is null || state.CurrentMap == _lastMap)
            return;

        // The first tick only records the live map: starting the app is not a map change.
        if (_lastMap is not null)
        {
            _notifications.ShowMapChange(
                state.CurrentMap,
                $"Ranked map: {state.CurrentMap}",
                $"Until {state.EndsAt.ToLocalTime():HH:mm} · Next: {state.NextMap}");
        }
        _lastMap = state.CurrentMap;
    }

    public void Dispose() => _messenger.UnregisterAll(this);
}
