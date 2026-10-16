using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using RippleTreasury.Api;
using RippleTreasury.Infrastructure;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot")
});
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1_048_576);
// ILogger output includes UTC timestamps and correlation/payment scopes.
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff 'UTC' ";
});
builder.Services.AddTreasuryApi(builder.Configuration, builder.Environment);

var app = builder.Build();
await using (var db = await app.Services.GetRequiredService<IDbContextFactory<TreasuryDb>>().CreateDbContextAsync())
    await TreasuryDatabaseInitializer.InitializeAsync(db);

app.UseMiddleware<CorrelationMiddleware>();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseBundledFrontend();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = x => x.Tags.Contains("ready") });
app.Run();

public partial class Program;
