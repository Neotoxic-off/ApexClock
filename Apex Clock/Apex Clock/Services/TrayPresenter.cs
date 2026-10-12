using ApexClock.Core;
using Apex_Clock.Formatting;
using Apex_Clock.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace Apex_Clock.Services;

/// <summary>Keeps the tray icon and tooltip in sync with the live map, window open or not.</summary>
public sealed class TrayPresenter : IRecipient<ClockTickMessage>, IDisposable
{
    private const string AppName = "Apex Clock";

    private readonly IMessenger _messenger;
    private readonly IScheduleStore _store;
    private readonly ITrayService _tray;

    public TrayPresenter(IMessenger messenger, IScheduleStore store, ITrayService tray)
    {
        _messenger = messenger;
        _store = store;
        _tray = tray;
        _messenger.RegisterAll(this);
    }

    public void Receive(ClockTickMessage message)
    {
        DateTimeOffset now = message.Value;
        RotationState? state = _store.Current.GetState(now);
        if (state is null)
        {
            _tray.SetStatus(AppName);
            _tray.SetLiveMap(null, 0);
            return;
        }

        _tray.SetStatus($"Ranked: {state.CurrentMap}\n{RotationText.TimeRange(state)}, {RotationText.Remaining(state, now)}\nNext: {state.NextMap}");
        _tray.SetLiveMap(state.CurrentMap, RotationText.Progress(state, now));
    }

    public void Dispose() => _messenger.UnregisterAll(this);
}
