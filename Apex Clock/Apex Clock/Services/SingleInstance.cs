namespace Apex_Clock.Services;

/// <summary>
/// One app per user session. A second launch signals the running instance, which raises
/// <see cref="ActivationRequested"/> (to show its window), then exits.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\ApexClock.Ui";
    private const string ActivateEventName = @"Local\ApexClock.Activate";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activate;
    private readonly RegisteredWaitHandle _registration;

    private SingleInstance(Mutex mutex)
    {
        _mutex = mutex;
        _activate = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
        _registration = ThreadPool.RegisterWaitForSingleObject(
            _activate,
            (object? state, bool timedOut) => ActivationRequested?.Invoke(this, EventArgs.Empty),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    /// <summary>Another launch asked for the window. Raised on a background thread.</summary>
    public event EventHandler? ActivationRequested;

    /// <summary>The instance lock, or null when another instance runs (it has then been asked to show itself).</summary>
    public static SingleInstance? TryAcquire()
    {
        Mutex mutex = new Mutex(initiallyOwned: true, MutexName, out bool isFirstInstance);
        if (isFirstInstance)
            return new SingleInstance(mutex);

        mutex.Dispose();
        if (EventWaitHandle.TryOpenExisting(ActivateEventName, out EventWaitHandle? activate))
        {
            using (activate)
                activate.Set();
        }
        return null;
    }

    public void Dispose()
    {
        _registration.Unregister(null);
        _activate.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
