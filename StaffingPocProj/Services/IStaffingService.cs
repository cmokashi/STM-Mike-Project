using StaffingPocProj.Models;

namespace StaffingPocProj.Services;

public interface IStaffingService
{
    Task<StaffingScreenDto?> GetScreenAsync(string staffId, int weeks, CancellationToken ct);

    Task<SaveWeekResult> SaveWeekHoursAsync(
        int scheduleItemKey, DateOnly weekEnding, decimal hours, CancellationToken ct);

    Task<SaveItemResult> SaveItemDetailsAsync(
        int scheduleItemKey, SaveItemDetailsRequest request, CancellationToken ct);
}
