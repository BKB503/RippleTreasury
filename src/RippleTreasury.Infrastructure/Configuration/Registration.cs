using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RippleTreasury.Application;

namespace RippleTreasury.Infrastructure;

public static class Registration
{

    /// <summary>Registers the tested provider adapters, routing/reference-data ports, use cases and differentiated HTTP/telemetry policies.</summary>
    public static IServiceCollection AddTreasury(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "Sqlite";
        if (provider != "Sqlite")
            throw new NotSupportedException("Only the tested SQLite adapter is implemented.");
        services.AddDbContextFactory<TreasuryDb>((sp, options) => options.UseSqlite(
            SqliteDatabaseConfiguration.BuildConnectionString(configuration,
                sp.GetService<IHostEnvironment>()?.ContentRootPath ?? AppContext.BaseDirectory)));
        services.AddSingleton<IAuditActorAccessor, SystemAuditActorAccessor>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IBatchIntakeStore, BatchIntakeRepository>();
        services.AddSingleton<PaymentExecutionRepository>();
        services.AddSingleton<IExecutionStore>(sp => sp.GetRequiredService<PaymentExecutionRepository>());
        services.AddSingleton<IReconciliationStore>(sp => sp.GetRequiredService<PaymentExecutionRepository>());
        services.AddSingleton<IPaymentReadRepository, PaymentReadRepository>();
        services.AddSingleton<IBatchQueries, BatchQueries>();
        services.AddSingleton<IPaymentQueries, PaymentQueries>();
        services.AddSingleton<IReconciliationQueries, ReconciliationQueries>();
        services.AddSingleton<IReportingQueries, ReportingQueries>();
        services.AddSingleton<IFundingAccountRepository, FundingAccountRepository>();
        services.AddSingleton<IBankCapabilityCatalog, DemoBankCapabilityCatalog>();
        services.AddSingleton<IBankRoutingPolicy, BankRoutingPolicy>();
        services.AddSingleton<TreasuryInstructionFactory>();
        services.AddSingleton<IFundingAccountQueries, FundingAccountQueries>();
        services.AddSingleton<IBankCapabilityQueries, BankCapabilityQueries>();
        services.AddSingleton<IBatchService, BatchService>();
        services.AddSingleton<DispatchService>();
        services.AddSingleton<ReconciliationService>();
        services.AddSingleton<MockBankEngine>();
        services.AddSingleton<IBankGateway, HttpBankGateway>();
        var address = new Uri(configuration["Banking:BaseUrl"] ?? "http://localhost:5080/");
        services.AddHttpClient("bank-submit", c => c.BaseAddress = address).AddStandardResilienceHandler(o =>
        {
            o.Retry.DisableForUnsafeHttpMethods();
            o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
            o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
        });
        services.AddHttpClient("bank-lookup", c => c.BaseAddress = address).AddStandardResilienceHandler(o =>
        {
            o.Retry.MaxRetryAttempts = 2;
            o.Retry.Delay = TimeSpan.FromMilliseconds(100);
            o.Retry.UseJitter = true;
            o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
            o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
        });
        var telemetry = services.AddOpenTelemetry().ConfigureResource(r => r.AddService(configuration["Observability:ServiceName"] ?? "RippleTreasury"))
            .WithTracing(t => t.AddSource(Telemetry.Name).AddHttpClientInstrumentation()).WithMetrics(m => m.AddMeter(Telemetry.Name));
        if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            telemetry.WithTracing(t => t.AddOtlpExporter()).WithMetrics(m => m.AddOtlpExporter());
        return services;
    }
}
