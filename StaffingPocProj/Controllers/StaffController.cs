using Microsoft.AspNetCore.Mvc;
using StaffingPocProj.Data;
using StaffingPocProj.Models;

namespace StaffingPocProj.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class StaffController(IStaffingRepository repository) : ControllerBase
{
    /// <summary>List of staff for the dropdown.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StaffDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<StaffDto>>> Get(CancellationToken ct)
        => Ok(await repository.GetStaffAsync(ct));
}