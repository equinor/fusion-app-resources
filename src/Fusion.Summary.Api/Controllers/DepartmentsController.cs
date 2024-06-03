using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Summary.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class DepartmentsController : ControllerBase
{
}
