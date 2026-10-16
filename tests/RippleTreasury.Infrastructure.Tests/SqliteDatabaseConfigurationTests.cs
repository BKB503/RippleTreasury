using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Infrastructure.Tests;

public class SqliteDatabaseConfigurationTests
{
    [Fact]
    public void Given_ConfiguredConnection_When_Resolved_Then_KeepOptionsAndResolveAgainstHostRoot()
    {
        var root = Path.GetTempPath();
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Treasury"] = "Data Source=data/treasury.db;Default Timeout=27;Foreign Keys=False"
        }).Build();

        var result = new SqliteConnectionStringBuilder(SqliteDatabaseConfiguration.BuildConnectionString(settings, root));

        Assert.Equal(Path.GetFullPath("data/treasury.db", root), result.DataSource);
        Assert.Equal(27, result.DefaultTimeout);
        Assert.True(result.ForeignKeys);
    }

    [Fact]
    public void Given_ExplicitPathOverride_When_Resolved_Then_PreserveIsolationAndEscapePath()
    {
        var path = Path.Combine(Path.GetTempPath(), "treasury;isolated.db");
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Treasury"] = "Data Source=default.db",
            ["Database:Path"] = path
        }).Build();

        var result = new SqliteConnectionStringBuilder(SqliteDatabaseConfiguration.BuildConnectionString(settings, Path.GetTempPath()));

        Assert.Equal(path, result.DataSource);
    }

    [Fact]
    public void Given_NoDatabaseSettings_When_Resolved_Then_FailClearly()
    {
        Assert.Throws<InvalidOperationException>(() => SqliteDatabaseConfiguration.BuildConnectionString(
            new ConfigurationBuilder().Build(), Path.GetTempPath()));
    }
}
