using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrbitalWatch.Api.Data;
using OrbitalWatch.Api.Hubs;
using OrbitalWatch.Api.Middleware;
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

// Authentication with JWT
builder.Services.AddScoped<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };

        // SignalR send the token as a query parameter (?access_token=...)
        // because browsers can't set Authorization headers on WebSocket upgrades.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    // Fixe window: 100 requests per minute for IP on API endpoints
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });

    // Stricter limit on the auth endpoint to prevent bute-force
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<AuditMiddleware>();

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapHub<TelemetryHub>("/hubs/telemetry");
app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory in integration tests
public abstract partial class Program
{
}