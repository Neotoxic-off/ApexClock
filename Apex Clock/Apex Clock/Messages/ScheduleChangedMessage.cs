using ApexClock.Core;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Apex_Clock.Messages;

/// <summary>A new rotation schedule was saved.</summary>
public sealed class ScheduleChangedMessage(RotationSchedule schedule) : ValueChangedMessage<RotationSchedule>(schedule);
