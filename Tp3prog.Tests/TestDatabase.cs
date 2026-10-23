using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tp3prog.DAO.EntityFramework;

namespace Tp3prog.Tests;

internal sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly DbContextOptions<AppDbContext> options;

    public TestDatabase()
    {
        connection.Open();
        options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public AppDbContext CreateContext() => new(options);

    public void Dispose() => connection.Dispose();
}
