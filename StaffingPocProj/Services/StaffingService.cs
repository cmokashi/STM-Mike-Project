using System.Globalization;
using StaffingPocProj.Data;
using StaffingPocProj.Models;

namespace StaffingPocProj.Services;

public sealed class StaffingService(IStaffingRepository repository, TimeProvider timeProvider) : IStaffingService
{
    private const int StandardWeekHours = 40;   // TODO: read from the Non-PCS standard hours table
    private const int MinWeeks = 1;
    private const int MaxWeeks = 52;

    public async Task<StaffingScreenDto?> GetScreenAsync(string staffId, int weeks, CancellationToken ct)
    {
        // Defensive checks: the controller validates too, but the service must not trust its callers.
        ArgumentException.ThrowIfNullOrWhiteSpace(staffId);
        staffId = staffId.Trim();

        var staffName = await repository.GetStaffNameAsync(staffId, ct);
        if (staffName is null)
            return null;

        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().Date);
        var weekEndings = WeekCalendar.GetWeekEndings(today, Math.Clamp(weeks, MinWeeks, MaxWeeks));
        var weekKeys = weekEndings
            .Select(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .ToList();

        var rows = await repository.GetStaffingRowsAsync(staffId, ct);

        foreach (var row in rows)
            foreach (var key in weekKeys)
                row.Hours[key] = null;

        if (rows.Count > 0)
        {
            var itemKeys = rows.Select(r => r.ScheduleItemKey).ToList();
            var weekHours = await repository.GetWeekHoursAsync(itemKeys, weekEndings[0], weekEndings[^1], ct);

            var byItem = rows.ToDictionary(r => r.ScheduleItemKey);
            foreach (var wh in weekHours)
            {
                if (byItem.TryGetValue(wh.ScheduleItemKey, out var row) && row.Hours.ContainsKey(wh.WeekEnding))
                    row.Hours[wh.WeekEnding] = (row.Hours[wh.WeekEnding] ?? 0) + wh.Hours;
            }
        }

        var weekTotals = weekKeys.ToDictionary(k => k, k => rows.Sum(r => r.Hours[k] ?? 0));

        var header = new StaffingHeaderDto(
            staffId,
            staffName,
            rows.Count,
            rows.Sum(r => r.BudgetHours ?? 0),
            weekTotals.Values.Sum());

        return new StaffingScreenDto(header, weekKeys, StandardWeekHours, weekTotals, rows);
    }

    public async Task<SaveWeekResult> SaveWeekHoursAsync(
        int scheduleItemKey, DateOnly weekEnding, decimal hours, CancellationToken ct)
    {
        // Business rules. The controller checks the range too, but the service must not trust its callers.
        if (hours < 0 || hours > SaveWeekHoursRequest.MaxHoursPerWeek)
        {
            return new SaveWeekResult(SaveWeekStatus.Invalid, "hours",
                $"Hours must be between 0 and {SaveWeekHoursRequest.MaxHoursPerWeek}.");
        }

        if (decimal.Round(hours, 2) != hours)
        {
            return new SaveWeekResult(SaveWeekStatus.Invalid, "hours",
                "Hours can have at most 2 decimal places.");
        }

        if (weekEnding.DayOfWeek != DayOfWeek.Friday)
        {
            return new SaveWeekResult(SaveWeekStatus.Invalid, "weekEnding",
                "Week ending date must be a Friday.");
        }

        // Editable window = the same weeks the screen shows: this week's Friday up to MaxWeeks ahead.
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().Date);
        var firstWeek = WeekCalendar.GetWeekEndings(today, 1)[0];
        var lastWeek = firstWeek.AddDays(7 * (MaxWeeks - 1));
        if (weekEnding < firstWeek || weekEnding > lastWeek)
        {
            return new SaveWeekResult(SaveWeekStatus.Invalid, "weekEnding",
                $"Week ending must be between {firstWeek:yyyy-MM-dd} and {lastWeek:yyyy-MM-dd}.");
        }

        if (!await repository.ScheduleItemExistsAsync(scheduleItemKey, ct))
            return new SaveWeekResult(SaveWeekStatus.ItemNotFound);

        // 0 hours = empty cell: remove the row instead of storing a zero.
        if (hours == 0)
        {
            await repository.DeleteWeekHoursAsync(scheduleItemKey, weekEnding, ct);
            return new SaveWeekResult(SaveWeekStatus.Cleared);
        }

        await repository.UpsertWeekHoursAsync(scheduleItemKey, weekEnding, hours, ct);
        return new SaveWeekResult(SaveWeekStatus.Saved);
    }

    public async Task<SaveItemResult> SaveItemDetailsAsync(
        int scheduleItemKey, SaveItemDetailsRequest request, CancellationToken ct)
    {
        // Blank text is stored as NULL, so the screen never shows "empty strings" that look like data.
        var staffComments = NormalizeText(request.StaffComments);
        var reviewerComments = NormalizeText(request.ReviewerComments);

        if (staffComments?.Length > SaveItemDetailsRequest.MaxCommentLength)
        {
            return new SaveItemResult(SaveItemStatus.Invalid, "staffComments",
                $"Staff comments can be at most {SaveItemDetailsRequest.MaxCommentLength} characters.");
        }

        if (reviewerComments?.Length > SaveItemDetailsRequest.MaxCommentLength)
        {
            return new SaveItemResult(SaveItemStatus.Invalid, "reviewerComments",
                $"Reviewer comments can be at most {SaveItemDetailsRequest.MaxCommentLength} characters.");
        }

        if (request.HoursToGo is { } hoursToGo)
        {
            if (hoursToGo < 0 || hoursToGo > SaveItemDetailsRequest.MaxHoursToGo)
            {
                return new SaveItemResult(SaveItemStatus.Invalid, "hoursToGo",
                    $"Hours to go must be between 0 and {SaveItemDetailsRequest.MaxHoursToGo}.");
            }

            if (decimal.Round(hoursToGo, 2) != hoursToGo)
            {
                return new SaveItemResult(SaveItemStatus.Invalid, "hoursToGo",
                    "Hours to go can have at most 2 decimal places.");
            }
        }

        if (!await repository.ScheduleItemExistsAsync(scheduleItemKey, ct))
            return new SaveItemResult(SaveItemStatus.ItemNotFound);

        if (request.PriorityKey is { } priorityKey && !await repository.PriorityExistsAsync(priorityKey, ct))
        {
            return new SaveItemResult(SaveItemStatus.Invalid, "priorityKey",
                $"Priority {priorityKey} does not exist.");
        }

        await repository.SaveItemDetailsAsync(
            scheduleItemKey, request.PriorityKey, staffComments, reviewerComments, request.HoursToGo, ct);

        return new SaveItemResult(
            SaveItemStatus.Saved,
            Saved: new SaveItemDetailsResponse(
                scheduleItemKey, request.PriorityKey, staffComments, reviewerComments, request.HoursToGo));
    }

    private static string? NormalizeText(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
