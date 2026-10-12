using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Apex_Clock.Messages;

/// <summary>Once a second on the UI thread: time to refresh countdowns.</summary>
public sealed class ClockTickMessage(DateTimeOffset now) : ValueChangedMessage<DateTimeOffset>(now);
