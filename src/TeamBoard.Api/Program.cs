using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TeamBoard.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ConnectionStrings:Default — overridden by the ConnectionStrings__Default env var (set by docker-compose.yml).
// For `dotnet run`, the sa password stays out of appsettings.json: set it with `dotnet user-secrets set DbPassword <password>`.
var connectionString = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Default"));
if (builder.Configuration["DbPassword"] is { Length: > 0 } dbPassword)
    connectionString.Password = dbPassword;

builder.Services.AddDbContext<TeamBoardDbContext>(options =>
    options.UseSqlServer(connectionString.ConnectionString));

// /health opens a connection to the database: Healthy (200) or Unhealthy (503).
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TeamBoardDbContext>("database");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply pending migrations on startup so the database is ready for Swagger testing.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<TeamBoardDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
