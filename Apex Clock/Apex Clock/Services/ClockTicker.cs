using Apex_Clock.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace Apex_Clock.Services;

/// <summary>Single app-wide clock: publishes a <see cref="ClockTickMessage"/> every second on the UI thread.</summary>
public sealed class ClockTicker(IMessenger messenger, TimeProvider time) : IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    /// <remarks>Call on the UI thread: the loop resumes on the captured context.</remarks>
    public void Start() => _ = RunAsync(_cts.Token);

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(Interval, time);
        try
        {
            do
                messenger.Send(new ClockTickMessage(time.GetUtcNow()));
            while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose() => _cts.Cancel();
}
