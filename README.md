# orbital-watch
> a real-time telemetry dashboard 
> 
> a full-stack app (React + .NET Core API) streaming mock satellite state vectors via SignalR, with SQL Server schema, Redis cache, and a Three.js ground track.

## Quick Setup and Run

```bash
# Clone the repository
git clone https://github.com/NathanJGaul/orbital-watch.git
cd orbital-watch

# Initialize the database schema
dotnet ef migrations add InitialSchema --project OrbitalWatch.Api
dotnet ef database update --project OrbitalWatch.Api

# Run web api
cd OrbitalWatch.Api/
dotnet run

# Test web api
curl http://localhost:5164/api/satellites | jq . # returns all satellites
curl http://localhost:5164/api/satellites/1 | jq . # returns a single satellite with given id
```

## Architecture Decisions

### C#/.NET

### SQLite / SQL Server

SQLite in development for quick, no setup, file-based database.

SQL Server in production for its temporal tables for TelemetryEvents and columnstore indexes for analytics queries on telemetry history.

### Entity Framework (EF)

### Dependency Injection (DI)

## Development

### System Requirements

Below are the prerequisits that are nessessary to start development on this project. The specific versions listed are the ones I am using but other versions may work.

```bash
dotnet --version      # 10.0.301
dotnet ef --version   # 10.0.9
docker --version      # 29.5.2
```

### Migrations

Migrations are code-controlled, reversible descriptions of a schema change. Each migration contains an Up() and Down() function performing the actions of `applying` and `rolling back` the migration respectively. Migrations should live in source control and gives you a complete audit trail of schema changes over time. They also allow for easier sharing of db migrations between members on a team.

Creating a migration from a DB Context file:

```bash
dotnet ef migrations add InitialSchema --project OrbitalWatch.Api
```

Update database schema after a new migration has been made within a project:

```bash
dotnet ef database update --project OrbitalWatch.Api
```
