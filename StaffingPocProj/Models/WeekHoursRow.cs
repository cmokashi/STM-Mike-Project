namespace StaffingPocProj.Models;

public sealed class WeekHoursRow
{
    public int ScheduleItemKey { get; set; }
    public string WeekEnding { get; set; } = "";
    public decimal Hours { get; set; }
}