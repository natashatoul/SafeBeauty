using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SafeBeauty.API.Data;

namespace SafeBeauty.API.Tests;

public sealed class TestDatabase : IDisposable
{
    private const string NormalizationMigration =
        "20260709085218_AddNormalizedIngredientName";

    private readonly SqliteConnection _connection;

    public SafeBeautyDbContext Context { get; }

    public TestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SafeBeautyDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new SafeBeautyDbContext(options);

        // Reproduce the real upgrade state: the normalized-name column exists,
        // but the unique index has not been applied yet.
        var migrator = Context.GetService<IMigrator>();
        migrator.Migrate(NormalizationMigration);
    }

    public void ApplyRemainingMigrations()
    {
        Context.GetService<IMigrator>().Migrate();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
