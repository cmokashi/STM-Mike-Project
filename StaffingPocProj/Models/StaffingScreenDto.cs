namespace StaffingPocProj.Models;

public sealed record StaffingHeaderDto(
    string StaffId,
    string StaffName,
    int Items,
    decimal TotalBudgetHours,
    decimal TotalPlannedHours);

public sealed record StaffingScreenDto(
    StaffingHeaderDto Header,
    IReadOnlyList<string> Weeks,
    int StandardWeekHours,
    IReadOnlyDictionary<string, decimal> WeekTotals,
    IReadOnlyList<StaffingRow> Rows);