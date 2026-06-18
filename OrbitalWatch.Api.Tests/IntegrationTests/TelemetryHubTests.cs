using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrbitalWatch.Api.Data;
using OrbitalWatch.Api.Hubs;
using OrbitalWatch.Api.Models;
using OrbitalWatch.Api.Services;
using StackExchange.Redis;

namespace OrbitalWatch.Api.Tests.IntegrationTests;

public class TelemetryHubTests(TelemetryHubTestFixture fixture)
    : IClassFixture<TelemetryHubTestFixture>
{
    [Fact]
    public async Task Negotiate_ReturnsConnectionIdAndTransports()
    {
        var client = fixture.Factory.CreateClient();
        var token = GenerateTestToken();

        var response = await client.PostAsync($"/hubs/telemetry/negotiate?access_token={token}", null);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("connectionId", out var connId));
        Assert.NotNull(connId.GetString());
        Assert.NotEmpty(connId.GetString()!);

        Assert.True(root.TryGetProperty("availableTransports", out var transports));
        Assert.NotEqual(0, transports.GetArrayLength());
    }

    [Fact]
    public async Task Connect_HandshakeSucceeds()
    {
        var connection = CreateConnection();

        await connection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, connection.State);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Subscribe_ReceivesAcknowledgement()
    {
        var connection = CreateConnection();
        var subscribed = new TaskCompletionSource<int>();

        connection.On<int>("Subscribed", id => subscribed.TrySetResult(id));

        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeToSatellite", 1);

        var satId = await subscribed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, satId);

        await connection.InvokeAsync("UnsubscribeFromSatellite", 1);
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Subscribe_ReceivesTelemetryUpdate()
    {
        var connection = CreateConnection();
        var telemetryReceived = new TaskCompletionSource<TelemetryEvent>();

        connection.On<TelemetryEvent>("TelemetryUpdate", e => telemetryReceived.TrySetResult(e));

        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeToSatellite", 1);

        // Inject a telemetry event directly via the hub context (bypasses Redis)
        var hubContext = fixture.Factory.Services
            .GetRequiredService<IHubContext<TelemetryHub>>();
        var evt = new TelemetryEvent
        {
            Id = 99,
            SatelliteId = 1,
            Timestamp = DateTime.UtcNow,
            LatitudeDeg = 42.5,
            LongitudeDeg = -71.2,
            AltitudeKm = 500.0,
            VelocityXKms = 1.2,
            VelocityYKms = 3.4,
            VelocityZKms = 5.6,
            SpeedKms = 7.8,
        };
        await hubContext.Clients.Group(TelemetryHub.GroupName(1))
            .SendAsync("TelemetryUpdate", evt);

        var received = await telemetryReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(received);
        Assert.Equal(1, received.SatelliteId);
        Assert.Equal(42.5, received.LatitudeDeg);

        await connection.InvokeAsync("UnsubscribeFromSatellite", 1);
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Subscribe_ReceivesMultipleTelemetryUpdates()
    {
        var connection = CreateConnection();
        var count = 0;
        var receivedThree = new TaskCompletionSource();

        connection.On<TelemetryEvent>("TelemetryUpdate", _ =>
        {
            if (Interlocked.Increment(ref count) >= 3)
                receivedThree.TrySetResult();
        });

        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeToSatellite", 1);

        var hubContext = fixture.Factory.Services
            .GetRequiredService<IHubContext<TelemetryHub>>();

        for (int i = 0; i < 3; i++)
        {
            var evt = new TelemetryEvent
            {
                Id = 100 + i,
                SatelliteId = 1,
                Timestamp = DateTime.UtcNow,
                LatitudeDeg = 40.0 + i,
                LongitudeDeg = -70.0,
                AltitudeKm = 500.0,
                VelocityXKms = 1.2,
                VelocityYKms = 3.4,
                VelocityZKms = 5.6,
                SpeedKms = 7.8,
            };
            await hubContext.Clients.Group(TelemetryHub.GroupName(1))
                .SendAsync("TelemetryUpdate", evt);
        }

        await receivedThree.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(count >= 3);

        await connection.InvokeAsync("UnsubscribeFromSatellite", 1);
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Unsubscribe_StopsReceivingUpdates()
    {
        var connection = CreateConnection();
        var count = 0;
        var firstUpdateReceived = new TaskCompletionSource();

        connection.On<TelemetryEvent>("TelemetryUpdate", _ =>
        {
            Interlocked.Increment(ref count);
            firstUpdateReceived.TrySetResult();
        });

        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeToSatellite", 1);

        var hubContext = fixture.Factory.Services
            .GetRequiredService<IHubContext<TelemetryHub>>();

        // Send one update to confirm subscription works
        await hubContext.Clients.Group(TelemetryHub.GroupName(1))
            .SendAsync("TelemetryUpdate", new TelemetryEvent
            {
                Id = 1, SatelliteId = 1, Timestamp = DateTime.UtcNow,
                LatitudeDeg = 42, LongitudeDeg = -71, AltitudeKm = 500,
                VelocityXKms = 1, VelocityYKms = 2, VelocityZKms = 3, SpeedKms = 7,
            });
        await firstUpdateReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, count);

        // Unsubscribe
        await connection.InvokeAsync("UnsubscribeFromSatellite", 1);
        var countBeforeWait = count;

        // Send more updates — they should not reach the client
        for (int i = 0; i < 3; i++)
        {
            await hubContext.Clients.Group(TelemetryHub.GroupName(1))
                .SendAsync("TelemetryUpdate", new TelemetryEvent
                {
                    Id = 10 + i, SatelliteId = 1, Timestamp = DateTime.UtcNow,
                    LatitudeDeg = 43, LongitudeDeg = -72, AltitudeKm = 500,
                    VelocityXKms = 1, VelocityYKms = 2, VelocityZKms = 3, SpeedKms = 7,
                });
        }

        // Small delay to let any stray messages arrive
        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.Equal(countBeforeWait, count);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeToMultipleSatellites_ReceivesUpdatesForEach()
    {
        var connection = CreateConnection();
        var seenSatellites = new HashSet<int>();
        var receivedBoth = new TaskCompletionSource();

        connection.On<TelemetryEvent>("TelemetryUpdate", e =>
        {
            lock (seenSatellites)
            {
                seenSatellites.Add(e.SatelliteId);
                if (seenSatellites.Count >= 2)
                    receivedBoth.TrySetResult();
            }
        });

        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeToSatellite", 1);
        await connection.InvokeAsync("SubscribeToSatellite", 2);

        var hubContext = fixture.Factory.Services
            .GetRequiredService<IHubContext<TelemetryHub>>();

        var baseEvent = new TelemetryEvent
        {
            Id = 1, Timestamp = DateTime.UtcNow,
            LatitudeDeg = 42, LongitudeDeg = -71, AltitudeKm = 500,
            VelocityXKms = 1, VelocityYKms = 2, VelocityZKms = 3, SpeedKms = 7,
        };

        foreach (var satId in new[] { 1, 2 })
        {
            var evt = new TelemetryEvent
            {
                Id = satId,
                SatelliteId = satId,
                Timestamp = baseEvent.Timestamp,
                LatitudeDeg = baseEvent.LatitudeDeg,
                LongitudeDeg = baseEvent.LongitudeDeg,
                AltitudeKm = baseEvent.AltitudeKm,
                VelocityXKms = baseEvent.VelocityXKms,
                VelocityYKms = baseEvent.VelocityYKms,
                VelocityZKms = baseEvent.VelocityZKms,
                SpeedKms = baseEvent.SpeedKms,
            };
            await hubContext.Clients.Group(TelemetryHub.GroupName(satId))
                .SendAsync("TelemetryUpdate", evt);
        }

        await receivedBoth.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(seenSatellites.Count >= 2);

        await connection.InvokeAsync("UnsubscribeFromSatellite", 1);
        await connection.InvokeAsync("UnsubscribeFromSatellite", 2);
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task UnsubscribedSatellite_StopsUpdatesButOthersContinue()
    {
        var connection = CreateConnection();
        var receivedSat2 = new TaskCompletionSource();
        var receivedSat1AfterUnsub = false;

        connection.On<TelemetryEvent>("TelemetryUpdate", e =>
        {
            if (e.SatelliteId == 2) receivedSat2.TrySetResult();
            if (e.SatelliteId == 1) receivedSat1AfterUnsub = true;
        });

        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeToSatellite", 1);
        await connection.InvokeAsync("SubscribeToSatellite", 2);

        var hubContext = fixture.Factory.Services
            .GetRequiredService<IHubContext<TelemetryHub>>();

        // Confirm both subscriptions work
        await hubContext.Clients.Group(TelemetryHub.GroupName(2))
            .SendAsync("TelemetryUpdate", MakeEvent(2, 1));
        await receivedSat2.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Unsubscribe from satellite 1
        await connection.InvokeAsync("UnsubscribeFromSatellite", 1);

        // Send updates to both groups
        await hubContext.Clients.Group(TelemetryHub.GroupName(1))
            .SendAsync("TelemetryUpdate", MakeEvent(1, 99));
        await hubContext.Clients.Group(TelemetryHub.GroupName(2))
            .SendAsync("TelemetryUpdate", MakeEvent(2, 100));

        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.False(receivedSat1AfterUnsub, "Received update for unsubscribed satellite 1");

        await connection.InvokeAsync("UnsubscribeFromSatellite", 2);
        await connection.DisposeAsync();
    }

    private HubConnection CreateConnection()
    {
        var server = fixture.Factory.Server;
        var token = GenerateTestToken();
        return new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/telemetry",
                o =>
                {
                    o.HttpMessageHandlerFactory = _ => server.CreateHandler();
                    o.AccessTokenProvider = () => Task.FromResult(token)!;
                })
            .Build();
    }

    private string GenerateTestToken()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();
        return tokenService.GenerateToken("test-user");
    }

    private static TelemetryEvent MakeEvent(int satelliteId, int id) => new()
    {
        Id = id,
        SatelliteId = satelliteId,
        Timestamp = DateTime.UtcNow,
        LatitudeDeg = 42,
        LongitudeDeg = -71,
        AltitudeKm = 500,
        VelocityXKms = 1,
        VelocityYKms = 2,
        VelocityZKms = 3,
        SpeedKms = 7,
    };
}

public class TelemetryHubTestFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory { get; }
    private readonly string _dbPath;

    public TelemetryHubTestFixture()
    {
        _dbPath = $"test_orbital_{Guid.NewGuid():N}.db";

        // Create the database schema before the factory starts
        var ctxOpts = new DbContextOptionsBuilder<OrbitalWatchDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        using var schemaCtx = new OrbitalWatchDbContext(ctxOpts);
        schemaCtx.Database.EnsureCreated();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Environment", "Development");

                builder.UseSetting("Jwt:Key", "orbital-watch-dev-secret-key-change-in-production-min-32-chars");
                builder.UseSetting("Jwt:Issuer", "orbital-watch-api");
                builder.UseSetting("Jwt:Audience", "orbital-watch-client");
                builder.UseSetting("Jwt:ExpiryMinutes", "60");

                builder.ConfigureServices(services =>
                {
                    // Override database to a unique test file
                    var dbCtxOpts = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(DbContextOptions<OrbitalWatchDbContext>));
                    if (dbCtxOpts != null) services.Remove(dbCtxOpts);

                    var dbCtx = services.SingleOrDefault(d => d.ServiceType == typeof(OrbitalWatchDbContext));
                    if (dbCtx != null) services.Remove(dbCtx);

                    services.AddDbContext<OrbitalWatchDbContext>(opts =>
                        opts.UseSqlite($"Data Source={_dbPath}"));

                    // Replace Redis with mocks
                    var muxDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
                    if (muxDescriptor != null)
                        services.Remove(muxDescriptor);

                    var mux = new Mock<IConnectionMultiplexer>();
                    var sub = new Mock<ISubscriber>();
                    var db = new Mock<IDatabase>();

                    mux.Setup(m => m.GetSubscriber(It.IsAny<object?>())).Returns(sub.Object);
                    mux.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(db.Object);

                    sub.Setup(s => s.SubscribeAsync(
                            It.IsAny<RedisChannel>(),
                            It.IsAny<Action<RedisChannel, RedisValue>>(),
                            It.IsAny<CommandFlags>()))
                        .Returns(Task.CompletedTask);

                    sub.Setup(s => s.PublishAsync(
                            It.IsAny<RedisChannel>(),
                            It.IsAny<RedisValue>(),
                            It.IsAny<CommandFlags>()))
                        .ReturnsAsync(0L);

                    db.Setup(d => d.StringGetAsync(
                            It.IsAny<RedisKey>(),
                            It.IsAny<CommandFlags>()))
                        .ReturnsAsync(RedisValue.Null);

                    db.Setup(d => d.StringSetAsync(
                            It.IsAny<RedisKey>(),
                            It.IsAny<RedisValue>(),
                            It.IsAny<TimeSpan?>(),
                            It.IsAny<bool>(),
                            It.IsAny<When>(),
                            It.IsAny<CommandFlags>()))
                        .ReturnsAsync(true);

                    services.AddSingleton<IConnectionMultiplexer>(mux.Object);
                });
            });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        Factory.Dispose();
        try
        {
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }
        catch
        {
            /* best effort cleanup */
        }

        return Task.CompletedTask;
    }
}