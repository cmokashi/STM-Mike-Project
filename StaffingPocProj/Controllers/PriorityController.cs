
using Microsoft.AspNetCore.Mvc;
using StaffingPocProj.Data;
using StaffingPocProj.Models;

namespace StaffingPocProj.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class PriorityController(IStaffingRepository repository) : ControllerBase
{
    /// <summary>Priority choices for the dropdown.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PriorityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<PriorityDto>>> Get(CancellationToken ct)
        => Ok(await repository.GetPrioritiesAsync(ct));
}
