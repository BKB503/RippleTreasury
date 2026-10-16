using Microsoft.AspNetCore.Mvc;
using RippleTreasury.Application;

namespace RippleTreasury.Api.Controllers;

[ApiController]
[Route("api/v1/demo/users")]
public class DemoUsersController(IHostEnvironment environment) : ControllerBase
{
    /// <summary>Returns known fictional Development operator profiles; these are not authenticated identities.</summary>
    [HttpGet]

    public ActionResult<IReadOnlyList<AuditActor>> List() => environment.IsDevelopment() ? Ok(DemoUsers.All) : NotFound();
}
