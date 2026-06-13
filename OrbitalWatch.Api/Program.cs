using Microsoft.EntityFrameworkCore;
using OrbitalWatch.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register DB Context with SQLite
builder.Services.AddDbContext<OrbitalWatchDbContext>(options =>
  options.UseSqlite(builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=orbital_watch.db"));

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
