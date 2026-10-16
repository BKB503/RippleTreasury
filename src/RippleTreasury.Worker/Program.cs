using Microsoft.EntityFrameworkCore;
using RippleTreasury.Infrastructure;
var builder = Host.CreateApplicationBuilder(args);
// ILogger output includes UTC timestamps and correlation/payment scopes.
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff 'UTC' ";
});
builder.Services.AddTreasury(builder.Configuration);
builder.Services.AddHostedService<DispatchWorker>();
var host = builder.Build();
await using (var db = await host.Services.GetRequiredService<IDbContextFactory<TreasuryDb>>().CreateDbContextAsync()) await TreasuryDatabaseInitializer.InitializeAsync(db);
await host.RunAsync();

