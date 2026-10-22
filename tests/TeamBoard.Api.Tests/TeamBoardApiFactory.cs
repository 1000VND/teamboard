using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using TeamBoard.Infrastructure.Persistence;

namespace TeamBoard.Api.Tests;

// Runs the real API in memory, with EF InMemory instead of SQL Server so tests need no Docker.
public class TeamBoardApiFactory : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _databaseName = $"TeamBoardTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            // Drop the SQL Server registration (options + EF Core 9+ configuration callbacks) before adding InMemory
            services.RemoveAll(d =>
                d.ServiceType == typeof(DbContextOptions<TeamBoardDbContext>) ||
                d.ServiceType == typeof(IDbContextOptionsConfiguration<TeamBoardDbContext>));

            services.AddDbContext<TeamBoardDbContext>(o => o.UseInMemoryDatabase(_databaseName));
        });
    }
}

internal static class ServiceCollectionExtensions
{
    public static void RemoveAll(this IServiceCollection services, Func<ServiceDescriptor, bool> predicate)
    {
        foreach (var descriptor in services.Where(predicate).ToList())
            services.Remove(descriptor);
    }
}
