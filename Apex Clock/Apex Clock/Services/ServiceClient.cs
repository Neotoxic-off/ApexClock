using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using ApexClock.Core;
using Apex_Clock.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace Apex_Clock.Services;

/// <summary>Keeps a connection to the Windows service, reconnecting as needed.</summary>
public sealed class ServiceClient : IRotationClient, IDisposable
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(2);

    private readonly IMessenger _messenger;
    private readonly SynchronizationContext _uiContext;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
    private StreamWriter? _writer;

    /// <remarks>Must be created on the UI thread: messages are posted back to it.</remarks>
    public ServiceClient(IMessenger messenger)
    {
        _messenger = messenger;
        _uiContext = SynchronizationContext.Current ?? new SynchronizationContext();
    }

    public void Start() => _ = Task.Run(() => RunAsync(_cts.Token));

    public async Task<bool> SendScheduleAsync(RotationSchedule schedule)
    {
        string line = PipeProtocol.Serialize(PipeMessage.SetSchedule(schedule));
        await _writeLock.WaitAsync();
        try
        {
            if (_writer is null)
                return false;
            await _writer.WriteLineAsync(line);
            await _writer.FlushAsync();
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using NamedPipeClientStream pipe = new NamedPipeClientStream(".", PipeProtocol.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                await pipe.ConnectAsync(ConnectTimeout, cancellationToken);

                using StreamReader reader = new StreamReader(pipe, PipeProtocol.Encoding, leaveOpen: true);
                _writer = new StreamWriter(pipe, PipeProtocol.Encoding, leaveOpen: true);
                Publish(new ConnectionChangedMessage(true));

                string? line;
                while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
                    Dispatch(line);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException)
            {
                // Service not running or connection lost: retry below.
            }
            finally
            {
                await _writeLock.WaitAsync(CancellationToken.None);
                _writer = null;
                _writeLock.Release();
                Publish(new ConnectionChangedMessage(false));
            }

            try
            {
                await Task.Delay(RetryDelay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private void Dispatch(string line)
    {
        PipeMessage? message;
        try
        {
            message = PipeProtocol.Deserialize(line);
        }
        catch (JsonException)
        {
            return;
        }

        switch (message?.Type)
        {
            case PipeMessage.SnapshotType:
                Publish(new SnapshotReceivedMessage(message));
                break;
            case PipeMessage.ErrorType:
                Publish(new ServiceErrorMessage(message.Error ?? "Unknown service error."));
                break;
        }
    }

    private void Publish<TMessage>(TMessage message) where TMessage : class =>
        _uiContext.Post((object? state) => _messenger.Send(message), null);

    public void Dispose() => _cts.Cancel();
}
