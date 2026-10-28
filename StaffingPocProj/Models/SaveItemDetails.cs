// ============================================================
// LOCATION : StaffingPocProj\Models\SaveItemDetails.cs
// ACTION   : NEW file
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace StaffingPocProj.Models;

/// <summary>
/// The editable fields of one schedule item (StaffingMaster). All four properties must be present in the
/// JSON body; send null to clear a value. This prevents a client from wiping a field by forgetting it.
/// </summary>
public sealed class SaveItemDetailsRequest
{
    // Staff_Comments and ReviewerComments are nvarchar(200) = 200 bytes = 100 characters.
    public const int MaxCommentLength = 100;
    public const int MaxHoursToGo = 1000;   // business rule: confirm the real limit with Mike

    /// <summary>Priority key from GET /api/Priority, or null for no priority.</summary>
    public required int? PriorityKey { get; init; }

    [StringLength(MaxCommentLength)]
    public required string? StaffComments { get; init; }

    [StringLength(MaxCommentLength)]
    public required string? ReviewerComments { get; init; }

    [Range(0, MaxHoursToGo)]
    public required decimal? HoursToGo { get; init; }
}

public sealed record SaveItemDetailsResponse(
    int ScheduleItemKey,
    int? PriorityKey,
    string? StaffComments,
    string? ReviewerComments,
    decimal? HoursToGo);

public enum SaveItemStatus
{
    Saved,
    ItemNotFound,
    Invalid
}

public sealed record SaveItemResult(
    SaveItemStatus Status,
    string? Field = null,
    string? Message = null,
    SaveItemDetailsResponse? Saved = null);
