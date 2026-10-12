using System.Collections.Concurrent;
using ApexClock.Core;

namespace ApexClock.Service;

/// <summary>Connected UI clients; pushes messages to all of them.</summary>
public sealed class ClientHub(ILogger<ClientHub> logger)
{
    private readonly ConcurrentDictionary<Guid, PipeClient> _clients = new ConcurrentDictionary<Guid, PipeClient>();

    public void Add(Guid id, PipeClient client) => _clients[id] = client;

    public void Remove(Guid id) => _clients.TryRemove(id, out PipeClient? _);

    public async Task BroadcastAsync(PipeMessage message, CancellationToken cancellationToken)
    {
        string line = PipeProtocol.Serialize(message);
        foreach (KeyValuePair<Guid, PipeClient> client in _clients)
        {
            try
            {
                await client.Value.SendLineAsync(line, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug(ex, "Client {Id} unreachable, removed.", client.Key);
                Remove(client.Key);
            }
        }
    }
}

public sealed class PipeClient(StreamWriter writer)
{
    private static readonly TimeSpan WriteTimeout = TimeSpan.FromSeconds(5);

    private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

    public async Task SendLineAsync(string line, CancellationToken cancellationToken)
    {
        // A client that stops reading must not stall the broadcast loop.
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(WriteTimeout);

        await _writeLock.WaitAsync(timeout.Token);
        try
        {
            await writer.WriteLineAsync(line.AsMemory(), timeout.Token);
            await writer.FlushAsync(timeout.Token);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
