# Data Flow

The flow of data within Orbital Watch is as follows:

```
TelemetrySimulatorService
  → writes to DB
  → publishes JSON to Redis channel "orbital:telemetry:{id}"
      ↓
RedisSubscriberService
  → receives from Redis
  → calls TelemetryHub.Clients.Group(id).SendAsync(...)
      ↓
Browser client
  → SignalR connection → receives "TelemetryUpdate" message
  → updates Zustand store → re-renders React components
```

The Redis Pub/Sub and SignalR bridge simplifies transport negotiation, group management, and reconnect handling.