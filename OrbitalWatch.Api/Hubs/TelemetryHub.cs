using Microsoft.AspNetCore.SignalR;

namespace OrbitalWatch.Api.Hubs;

public class TelemetryHub(ILogger<TelemetryHub> logger) : Hub
{
    /// <summary>
    /// Called by the client to start receiving telemetry for a specific satellite.
    /// </summary>
    public async Task SubscribeToSatellite(int satelliteId)
    {
        var groupName = GroupName(satelliteId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        logger.LogDebug("Connection {ConnId} subscribed to satellite {SatelliteId}.", Context.ConnectionId,
            satelliteId);

        // Acknowledge the subscription back to the caller
        await Clients.Caller.SendAsync("Subscribed", satelliteId);
    }

    /// <summary>
    /// Called by the client to stop receiving telemetry for a specific satellite.
    /// </summary>
    public async Task UnsubscribeFromSatellite(int satelliteId)
    {
        var groupName = GroupName(satelliteId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        logger.LogDebug("Connection {ConnId} unsubscribed from satellite {SatelliteId}.", Context.ConnectionId,
            satelliteId);
    }

    public override Task OnConnectedAsync()
    {
        logger.LogInformation(
            "Client connected: {ConnId} from {Ip}.",
            Context.ConnectionId,
            Context.GetHttpContext()?.Connection.RemoteIpAddress);
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation(
            "Client disconnected: {ConnId}. Reason: {Reason}",
            Context.ConnectionId,
            exception?.Message ?? "clean disconnect");
        await base.OnDisconnectedAsync(exception);
    }

    // Shared helper used for both the hub and the Redis bridge
    public static string GroupName(int satelliteId) => $"sat-{satelliteId}";
}