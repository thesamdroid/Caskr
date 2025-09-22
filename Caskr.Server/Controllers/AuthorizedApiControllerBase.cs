using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public abstract class AuthorizedApiControllerBase : ControllerBase
{
}
