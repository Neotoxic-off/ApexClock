using ApexClock.Core;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Apex_Clock.Messages;

/// <summary>The service pushed a new schedule/rotation snapshot.</summary>
public sealed class SnapshotReceivedMessage(PipeMessage snapshot) : ValueChangedMessage<PipeMessage>(snapshot);

/// <summary>The service rejected a request.</summary>
public sealed class ServiceErrorMessage(string error) : ValueChangedMessage<string>(error);

/// <summary>The connection to the service was opened or lost.</summary>
public sealed class ConnectionChangedMessage(bool isConnected) : ValueChangedMessage<bool>(isConnected);
