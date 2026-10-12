using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using ApexClock.Core;

namespace ApexClock.Service;

/// <summary>Accepts UI connections on the named pipe and applies schedule edits.</summary>
public sealed class PipeServer(ScheduleStore store, ClientHub hub, ILogger<PipeServer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            NamedPipeServerStream pipe;
            try
            {
                pipe = CreatePipe();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogError(ex, "Could not create pipe {Pipe}, retrying in 5 s.", PipeProtocol.PipeName);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            try
            {
                await pipe.WaitForConnectionAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                await pipe.DisposeAsync();
                break;
            }

            _ = HandleClientAsync(pipe, stoppingToken);
        }
    }

    private static NamedPipeServerStream CreatePipe()
    {
        // The service runs as LocalSystem while the UI runs as the logged-on user, so grant
        // authenticated users read/write. CreateNewInstance is withheld so they cannot host the pipe.
        PipeSecurity security = new PipeSecurity();
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
        // The running identity must keep CreateNewInstance to open the next instance (matters when run as a console app).
        using WindowsIdentity self = WindowsIdentity.GetCurrent();
        security.AddAccessRule(new PipeAccessRule(self.User!, PipeAccessRights.FullControl, AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            PipeProtocol.PipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous,
            inBufferSize: 0,
            outBufferSize: 0,
            security);
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken stoppingToken)
    {
        Guid id = Guid.NewGuid();
        await using (pipe)
        {
            try
            {
                using StreamReader reader = new StreamReader(pipe, PipeProtocol.Encoding, leaveOpen: true);
                await using StreamWriter writer = new StreamWriter(pipe, PipeProtocol.Encoding, leaveOpen: true);
                PipeClient client = new PipeClient(writer);
                hub.Add(id, client);

                await client.SendLineAsync(PipeProtocol.Serialize(PipeMessage.Snapshot(store.Current, DateTimeOffset.UtcNow)), stoppingToken);

                string? line;
                while ((line = await reader.ReadLineAsync(stoppingToken)) is not null)
                {
                    string? error = Apply(line);
                    if (error is not null)
                        await client.SendLineAsync(PipeProtocol.Serialize(PipeMessage.Failure(error)), stoppingToken);
                }
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException or ObjectDisposedException)
            {
                // Client disconnected or service stopping.
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on client connection {Id}.", id);
            }
            finally
            {
                hub.Remove(id);
            }
        }
    }

    /// <summary>Handles one client request; returns an error message for the client, or null.</summary>
    private string? Apply(string line)
    {
        PipeMessage? message;
        try
        {
            message = PipeProtocol.Deserialize(line);
        }
        catch (JsonException)
        {
            return "Unreadable message.";
        }

        if (message?.Type != PipeMessage.SetScheduleType)
            return $"Unknown request: {message?.Type}.";
        if (message.Schedule is null)
            return "Missing schedule.";

        RotationSchedule schedule = message.Schedule.Normalized();
        string? invalid = schedule.Validate();
        if (invalid is not null)
            return invalid;

        try
        {
            store.Save(schedule);
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "Could not save the schedule.");
            return "The service could not save the schedule.";
        }

        // RotationWorker notices the new schedule instance and broadcasts it.
        logger.LogInformation("Schedule updated: {Maps}.", string.Join(" → ", schedule.Entries.Select((RotationEntry e) => e.Map)));
        return null;
    }
}
