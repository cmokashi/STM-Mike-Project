// ============================================================
// LOCATION : StaffingPocProj\Models\SaveWeekHours.cs
// ACTION   : NEW file
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace StaffingPocProj.Models;

public sealed class SaveWeekHoursRequest
{
    public const int MaxHoursPerWeek = 80;   // business rule: confirm the real limit with Mike

    /// <summary>Hours planned for the week. 0 clears the cell (the row is removed).</summary>
    [Range(0, MaxHoursPerWeek)]
    public required decimal Hours { get; init; }
}

public sealed record SaveWeekHoursResponse(int ScheduleItemKey, string WeekEnding, decimal Hours, bool Cleared);

public enum SaveWeekStatus
{
    Saved,
    Cleared,
    ItemNotFound,
    Invalid
}

public sealed record SaveWeekResult(SaveWeekStatus Status, string? Field = null, string? Message = null);
