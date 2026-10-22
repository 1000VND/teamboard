using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamBoard.Application.Common;
using TeamBoard.Infrastructure.Persistence;

namespace TeamBoard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ConnectionStrings:Default — overridden by the ConnectionStrings__Default env var (set by docker-compose.yml).
        // For `dotnet run`, the sa password stays out of appsettings.json: set it with `dotnet user-secrets set DbPassword <password>`.
        var connectionString = new SqlConnectionStringBuilder(configuration.GetConnectionString("Default"));
        if (configuration["DbPassword"] is { Length: > 0 } dbPassword)
            connectionString.Password = dbPassword;

        services.AddDbContext<TeamBoardDbContext>(options =>
            options.UseSqlServer(connectionString.ConnectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<TeamBoardDbContext>());

        // /health opens a connection to the database: Healthy (200) or Unhealthy (503).
        services.AddHealthChecks()
            .AddDbContextCheck<TeamBoardDbContext>("database");

        return services;
    }

    // Applies pending migrations on startup so the database is ready for Swagger testing.
    public static void ApplyMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TeamBoardDbContext>();
        if (db.Database.IsRelational()) // the EF InMemory provider used by tests has no migrations
            db.Database.Migrate();
    }
}
