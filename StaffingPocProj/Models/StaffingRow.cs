namespace StaffingPocProj.Models;

public sealed class StaffingRow
{
    public int ScheduleItemKey { get; set; }
    public string? ProjectNumber { get; set; }
    public string? Client { get; set; }
    public string? Service { get; set; }
    public string? ReportTo { get; set; }
    public string? StartDate { get; set; }
    public string? DueDate { get; set; }
    public decimal? BudgetHours { get; set; }
    public decimal? ActualHours { get; set; }
    public string? TaxOrAA { get; set; }
    public string? Priority { get; set; }
    public string? StaffComments { get; set; }
    public string? ReviewerComments { get; set; }
    public decimal? HoursToGo { get; set; }

    // Key = week ending date (yyyy-MM-dd). Filled by the service from StaffingWeeks.
    public Dictionary<string, decimal?> Hours { get; set; } = new();
}