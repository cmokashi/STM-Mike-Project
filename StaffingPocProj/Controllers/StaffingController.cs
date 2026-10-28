using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using StaffingPocProj.Models;
using StaffingPocProj.Services;

namespace StaffingPocProj.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class StaffingController(IStaffingService service) : ControllerBase
{
    private const int MaxStaffIdLength = 10;   
    private const int MinWeeks = 1;
    private const int MaxWeeks = 52;

    /// <summary>Header, detail rows and weekly hours for one staff person.</summary>
    /// <param name="staffId">Staff id, for example BS.</param>
    /// <param name="weeks">Number of week columns (1 to 52). Default 26.</param>
    [HttpGet("{staffId}")]
    [ProducesResponseType(typeof(StaffingScreenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StaffingScreenDto>> Get(
        [Required, StringLength(MaxStaffIdLength, MinimumLength = 1)] string staffId,
        [FromQuery, Range(MinWeeks, MaxWeeks)] int weeks = 26,
        CancellationToken ct = default)
    {
        staffId = staffId.Trim();
        if (staffId.Length == 0)
        {
            ModelState.AddModelError(nameof(staffId), "Staff id is required.");
            return ValidationProblem(ModelState);
        }

        var screen = await service.GetScreenAsync(staffId, weeks, ct);
        if (screen is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Staff not found",
                Detail = $"Staff '{staffId}' was not found."
            });
        }

        return Ok(screen);
    }

    /// <summary>Save the hours for one schedule item in one week. Hours = 0 clears the cell.</summary>
    /// <param name="scheduleItemKey">Practice CS schedule item key (MYSTM.schedule_item_KEY).</param>
    /// <param name="weekEnding">Week ending date. Must be a Friday, for example 2026-10-30.</param>
    /// <param name="request">The hours (0 to 80, at most 2 decimals).</param>
    [HttpPut("items/{scheduleItemKey:int}/weeks/{weekEnding}")]
    [ProducesResponseType(typeof(SaveWeekHoursResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SaveWeekHoursResponse>> SaveWeekHours(
        [Range(1, int.MaxValue)] int scheduleItemKey,
        DateOnly weekEnding,
        [FromBody] SaveWeekHoursRequest request,
        CancellationToken ct = default)
    {
        var result = await service.SaveWeekHoursAsync(scheduleItemKey, weekEnding, request.Hours, ct);

        switch (result.Status)
        {
            case SaveWeekStatus.ItemNotFound:
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Schedule item not found",
                    Detail = $"Schedule item {scheduleItemKey} was not found."
                });

            case SaveWeekStatus.Invalid:
                ModelState.AddModelError(result.Field ?? "request", result.Message ?? "Invalid request.");
                return ValidationProblem(ModelState);

            default:
                return Ok(new SaveWeekHoursResponse(
                    scheduleItemKey,
                    weekEnding.ToString("yyyy-MM-dd"),
                    request.Hours,
                    result.Status == SaveWeekStatus.Cleared));
        }
    }

    /// <summary>Save the editable fields of one schedule item. All four fields are required; null clears a value.</summary>
    /// <param name="scheduleItemKey">Practice CS schedule item key (MYSTM.schedule_item_KEY).</param>
    /// <param name="request">Priority key, staff comments, reviewer comments and hours to go.</param>
    [HttpPut("items/{scheduleItemKey:int}")]
    [ProducesResponseType(typeof(SaveItemDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SaveItemDetailsResponse>> SaveItemDetails(
        [Range(1, int.MaxValue)] int scheduleItemKey,
        [FromBody] SaveItemDetailsRequest request,
        CancellationToken ct = default)
    {
        var result = await service.SaveItemDetailsAsync(scheduleItemKey, request, ct);

        switch (result.Status)
        {
            case SaveItemStatus.ItemNotFound:
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Schedule item not found",
                    Detail = $"Schedule item {scheduleItemKey} was not found."
                });

            case SaveItemStatus.Invalid:
                ModelState.AddModelError(result.Field ?? "request", result.Message ?? "Invalid request.");
                return ValidationProblem(ModelState);

            default:
                return Ok(result.Saved);
        }
    }
}
