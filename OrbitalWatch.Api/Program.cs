using Microsoft.EntityFrameworkCore;
using OrbitalWatch.Api.Data;
using OrbitalWatch.Api.Repositories;
using OrbitalWatch.Api.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register DB Context with SQLite
builder.Services.AddDbContext<OrbitalWatchDbContext>(options =>
  options.UseSqlite(builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=orbital_watch.db"));

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
  var connectionString = builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";
  return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddScoped<ISatelliteRepository, SatelliteRepository>(); // AddScoped creates one instance per HTTP request, which is required instead of AddSingleton as EF is not thread safe
builder.Services.AddScoped<ITelemetryRepository, TelemetryRepository>();

builder.Services.AddScoped<CurrentStateService>();

builder.Services.AddHostedService<SeedService>();

builder.Services.AddHostedService<TelemetrySimulatorService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
