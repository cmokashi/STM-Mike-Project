namespace StaffingPocProj.Services;

public static class WeekCalendar
{
    /// <summary>Returns <paramref name="count"/> week-ending dates; the first is this week's ending day (Friday).</summary>
    public static IReadOnlyList<DateOnly> GetWeekEndings(
        DateOnly today, int count, DayOfWeek weekEndsOn = DayOfWeek.Friday)
    {
        var daysUntilEnd = ((int)weekEndsOn - (int)today.DayOfWeek + 7) % 7;
        var first = today.AddDays(daysUntilEnd);
        return Enumerable.Range(0, count).Select(i => first.AddDays(7 * i)).ToList();
    }
}