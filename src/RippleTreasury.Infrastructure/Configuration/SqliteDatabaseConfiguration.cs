using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace RippleTreasury.Infrastructure;

/// <summary>Resolves host-relative SQLite configuration and preserves existing explicit path overrides.</summary>
public static class SqliteDatabaseConfiguration
{
    /// <summary>Safely escapes SQLite settings and always enforces financial foreign-key integrity.</summary>
    public static string BuildConnectionString(IConfiguration configuration, string contentRoot)
    {
        var configured = configuration.GetConnectionString("Treasury");
        var connection = new SqliteConnectionStringBuilder(configured ?? "") { ForeignKeys = true };
        if (configured is null)
            connection.DefaultTimeout = 10;

        // Existing scripts and isolated tests can continue to override Database:Path.
        var path = configuration["Database:Path"] ?? connection.DataSource;
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("Configure ConnectionStrings:Treasury or Database:Path.");

        connection.DataSource = path == ":memory:" || connection.Mode == SqliteOpenMode.Memory
            ? path
            : Path.GetFullPath(path, contentRoot);
        return connection.ToString();
    }
}
