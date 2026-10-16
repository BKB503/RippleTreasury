using System.Security.Claims;
using RippleTreasury.Application;

namespace RippleTreasury.Api;

public class RequestAuditActorAccessor(IHttpContextAccessor contexts, IHostEnvironment environment) : IAuditActorAccessor
{
    public AuditActor Current
    {
        get
        {
            var context = contexts.HttpContext;
            if (context?.User.Identity?.IsAuthenticated == true)
                return new(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub")
                    ?? throw new InvalidOperationException("Authenticated identity requires a stable subject."),
                    context.User.Identity.Name ?? "Authenticated user", "User");
            if (context is null)
                return AuditActor.System("system-client");
            if (!environment.IsDevelopment())
                return new("anonymous", "Unauthenticated caller", "Anonymous");
            var selected = context.Request.Headers["X-Demo-User"].ToString();
            if (string.IsNullOrEmpty(selected))
                return DemoUsers.All[0];
            return DemoUsers.All.SingleOrDefault(x => x.Id == selected)
                ?? throw new InputException(new()
                {
                    ["X-Demo-User"] = ["Select a known demo user."]
                });
        }
    }
}
