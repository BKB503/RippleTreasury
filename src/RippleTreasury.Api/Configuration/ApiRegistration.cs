using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;
using RippleTreasury.Application;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Api;

public static class ApiRegistration
{

    public static IServiceCollection AddTreasuryApi(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddTreasury(configuration);
        services.AddHttpContextAccessor();
        services.AddSingleton<IAuditActorAccessor, RequestAuditActorAccessor>();
        services.AddOpenTelemetry().WithTracing(t => t.AddAspNetCoreInstrumentation());
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddControllers().ConfigureApiBehaviorOptions(options =>
        {
            // Keep binding failures on the same central ProblemDetails path as application validation.
            options.InvalidModelStateResponseFactory = context => throw new InputException(
                context.ModelState.Where(x => x.Value?.Errors.Count > 0).ToDictionary(x => x.Key,
                    x => x.Value!.Errors.Select(_ => "Invalid or missing request value.").ToArray()));
        });
        services.AddSwaggerGen();
        services.AddHealthChecks().AddCheck<DatabaseHealth>("database", tags: ["ready"]);
        if (configuration.GetValue("Processing:EmbeddedWorker", true))
            services.AddHostedService<DispatchWorker>();
        if (environment.IsDevelopment() && configuration.GetValue("Frontend:OpenBrowser", false))
            services.AddHostedService<DevelopmentBrowserLauncher>();
        return services;
    }
}
