using StaffingPocProj.Models;

namespace StaffingPocProj.Data;

public interface IStaffingRepository
{
    Task<IReadOnlyList<StaffDto>> GetStaffAsync(CancellationToken ct);
    Task<string?> GetStaffNameAsync(string staffId, CancellationToken ct);
    Task<IReadOnlyList<StaffingRow>> GetStaffingRowsAsync(string staffId, CancellationToken ct);
    Task<IReadOnlyList<WeekHoursRow>> GetWeekHoursAsync(
        IReadOnlyCollection<int> scheduleItemKeys, DateOnly from, DateOnly to, CancellationToken ct);

    Task<bool> ScheduleItemExistsAsync(int scheduleItemKey, CancellationToken ct);
    Task UpsertWeekHoursAsync(int scheduleItemKey, DateOnly weekEnding, decimal hours, CancellationToken ct);
    Task DeleteWeekHoursAsync(int scheduleItemKey, DateOnly weekEnding, CancellationToken ct);

    Task<IReadOnlyList<PriorityDto>> GetPrioritiesAsync(CancellationToken ct);
    Task<bool> PriorityExistsAsync(int priorityKey, CancellationToken ct);
    Task SaveItemDetailsAsync(
        int scheduleItemKey, int? priorityKey, string? staffComments, string? reviewerComments,
        decimal? hoursToGo, CancellationToken ct);
}
