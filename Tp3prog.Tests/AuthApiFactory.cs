using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tp3prog.Controllers;
using Tp3prog.DAO.EntityFramework;

[assembly: WebApplicationFactoryContentRoot("Tp3prog", "../../../../Tp3prog", "Tp3prog.csproj", "0")]

namespace Tp3prog.Tests;

internal sealed class AuthApiFactory(IReadOnlyDictionary<string, string?>? overrides = null) : WebApplicationFactory<CoursesController>
{
    internal const string SigningKey = "Test-only-signing-key-for-authentication-2026-not-a-production-secret";
    internal const string Issuer = "Tp3prog.Tests";
    internal const string Audience = "Tp3prog.TestClient";
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Unused: SQLite replaces the database for tests",
                ["Jwt:Key"] = SigningKey,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:ExpirationMinutes"] = "60"
            };
            if (overrides is not null)
                foreach (var setting in overrides) settings[setting.Key] = setting.Value;
            configuration.AddInMemoryCollection(settings);
        });
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }
}
