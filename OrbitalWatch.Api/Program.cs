using Microsoft.EntityFrameworkCore;
using OrbitalWatch.Api.Data;
using OrbitalWatch.Api.Hubs;
using OrbitalWatch.Api.Repositories;
using OrbitalWatch.Api.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Framework services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Data infrastructure
builder.Services.AddDbContext<OrbitalWatchDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=orbital_watch.db"));

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connectionString);
});

// Repositories
builder.Services
    .AddScoped<ISatelliteRepository,
        SatelliteRepository>(); // AddScoped creates one instance per HTTP request, which is required instead of AddSingleton as EF is not thread safe
builder.Services.AddScoped<ITelemetryRepository, TelemetryRepository>();

// Application services
builder.Services.AddScoped<CurrentStateService>();

// Realtime
builder.Services.AddSignalR();

// Background services
builder.Services.AddHostedService<SeedService>();
builder.Services.AddHostedService<TelemetrySimulatorService>();
builder.Services.AddHostedService<RedisSubscriberService>();

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Endpoints
app.MapHub<TelemetryHub>("/hubs/telemetry");
app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory in integration tests
public partial class Program
{
}