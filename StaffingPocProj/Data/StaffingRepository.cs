using System.Data;
using Dapper;
using StaffingPocProj.Models;

namespace StaffingPocProj.Data;

public sealed class StaffingRepository(IDbConnectionFactory connectionFactory) : IStaffingRepository
{
    public async Task<IReadOnlyList<StaffDto>> GetStaffAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT DISTINCT RTRIM(staff_id) AS StaffId, StaffName
            FROM dbo.MYSTM
            ORDER BY StaffName;
            """;

        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<StaffDto>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<string?> GetStaffNameAsync(string staffId, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (1) StaffName
            FROM dbo.MYSTM
            WHERE RTRIM(staff_id) = @staffId;
            """;

        using var connection = connectionFactory.Create();
        return await connection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(sql, new { staffId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<StaffingRow>> GetStaffingRowsAsync(string staffId, CancellationToken ct)
    {
        // MYSTM = read-only Practice CS snapshot. StaffingMaster = our own editable data.
        const string sql = """
            SELECT m.schedule_item_KEY                                    AS ScheduleItemKey,
                   CAST(m.project_number AS nvarchar(50))                 AS ProjectNumber,
                   m.ClientName                                           AS Client,
                   m.ActivityCode                                         AS Service,
                   m.ReportsTo                                            AS ReportTo,
                   CONVERT(char(10), m.target_start_date_utc, 23)         AS StartDate,
                   CONVERT(char(10), DATEADD(DAY, -1, m.target_complete_date_exclusive_utc), 23) AS DueDate,
                   TRY_CAST(m.budgeted_hours AS decimal(18,4))            AS BudgetHours,
                   TRY_CAST(m.live_actual_hours AS decimal(18,4))         AS ActualHours,
                   m.WT                                                   AS TaxOrAA,
                   CAST(sm.PriorityKey AS nvarchar(50))                   AS Priority,
                   CAST(sm.Staff_Comments AS nvarchar(max))               AS StaffComments,
                   CAST(sm.ReviewerComments AS nvarchar(max))             AS ReviewerComments,
                   TRY_CAST(sm.HourstoGo AS decimal(18,4))                AS HoursToGo
            FROM dbo.MYSTM m
            LEFT JOIN dbo.StaffingMaster sm ON sm.Schedule_itemkey = m.schedule_item_KEY
            WHERE RTRIM(m.staff_id) = @staffId
            ORDER BY m.ClientName, m.project_number;
            """;

        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<StaffingRow>(
            new CommandDefinition(sql, new { staffId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<WeekHoursRow>> GetWeekHoursAsync(
        IReadOnlyCollection<int> scheduleItemKeys, DateOnly from, DateOnly to, CancellationToken ct)
    {
        const string sql = """
            SELECT Schedule_item_key                       AS ScheduleItemKey,
                   CONVERT(char(10), WeekEndingDate, 23)   AS WeekEnding,
                   hoursforweek                            AS Hours
            FROM dbo.StaffingWeeks
            WHERE Schedule_item_key IN @keys
              AND WeekEndingDate >= @from
              AND WeekEndingDate <= @to;
            """;

        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<WeekHoursRow>(new CommandDefinition(
            sql,
            new
            {
                keys = scheduleItemKeys,
                from = from.ToDateTime(TimeOnly.MinValue),
                to = to.ToDateTime(TimeOnly.MinValue)
            },
            cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<bool> ScheduleItemExistsAsync(int scheduleItemKey, CancellationToken ct)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.MYSTM WHERE schedule_item_KEY = @scheduleItemKey)
                        THEN 1 ELSE 0 END;
            """;

        using var connection = connectionFactory.Create();
        var found = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { scheduleItemKey }, cancellationToken: ct));
        return found == 1;
    }

    public async Task UpsertWeekHoursAsync(int scheduleItemKey, DateOnly weekEnding, decimal hours, CancellationToken ct)
    {
        // No unique index on (Schedule_item_key, WeekEndingDate) yet, so the serializable
        // transaction is what prevents two parallel saves from inserting the same week twice.
        const string updateSql = """
            UPDATE dbo.StaffingWeeks
            SET hoursforweek = @hours
            WHERE Schedule_item_key = @scheduleItemKey AND WeekEndingDate = @week;
            """;
        const string insertSql = """
            INSERT INTO dbo.StaffingWeeks (Schedule_item_key, WeekEndingDate, hoursforweek)
            VALUES (@scheduleItemKey, @week, @hours);
            """;

        var args = new { scheduleItemKey, week = weekEnding.ToDateTime(TimeOnly.MinValue), hours };

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var updated = await connection.ExecuteAsync(
            new CommandDefinition(updateSql, args, transaction, cancellationToken: ct));
        if (updated == 0)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(insertSql, args, transaction, cancellationToken: ct));
        }

        await transaction.CommitAsync(ct);
    }

    public async Task DeleteWeekHoursAsync(int scheduleItemKey, DateOnly weekEnding, CancellationToken ct)
    {
        const string sql = """
            DELETE FROM dbo.StaffingWeeks
            WHERE Schedule_item_key = @scheduleItemKey AND WeekEndingDate = @week;
            """;

        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { scheduleItemKey, week = weekEnding.ToDateTime(TimeOnly.MinValue) },
            cancellationToken: ct));
    }

    public async Task<IReadOnlyList<PriorityDto>> GetPrioritiesAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT PriorityKEY AS PriorityKey, RTRIM(Description) AS Description
            FROM dbo.StaffingPriority
            ORDER BY PriorityKEY;
            """;

        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<PriorityDto>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<bool> PriorityExistsAsync(int priorityKey, CancellationToken ct)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.StaffingPriority WHERE PriorityKEY = @priorityKey)
                        THEN 1 ELSE 0 END;
            """;

        using var connection = connectionFactory.Create();
        var found = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { priorityKey }, cancellationToken: ct));
        return found == 1;
    }

    public async Task SaveItemDetailsAsync(
        int scheduleItemKey, int? priorityKey, string? staffComments, string? reviewerComments,
        decimal? hoursToGo, CancellationToken ct)
    {
        // Only the four editable fields are written. STAFFINIT (the staff assignment), Slider, MeetMax,
        // the week-ending dates and HowAllocated are never touched here.
        const string updateSql = """
            UPDATE dbo.StaffingMaster
            SET PriorityKey      = @priorityKey,
                Staff_Comments   = @staffComments,
                ReviewerComments = @reviewerComments,
                HourstoGo        = @hoursToGo
            WHERE Schedule_itemkey = @scheduleItemKey;
            """;

        // Safety net: a schedule item that is in MYSTM but not yet in StaffingMaster gets its row here.
        const string insertSql = """
            INSERT INTO dbo.StaffingMaster
                (Schedule_itemkey, STAFFINIT, PriorityKey, Staff_Comments, ReviewerComments, HourstoGo)
            SELECT TOP (1) m.schedule_item_KEY, m.staff_id, @priorityKey, @staffComments, @reviewerComments, @hoursToGo
            FROM dbo.MYSTM m
            WHERE m.schedule_item_KEY = @scheduleItemKey;
            """;

        var args = new { scheduleItemKey, priorityKey, staffComments, reviewerComments, hoursToGo };

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var updated = await connection.ExecuteAsync(
            new CommandDefinition(updateSql, args, transaction, cancellationToken: ct));
        if (updated == 0)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(insertSql, args, transaction, cancellationToken: ct));
        }

        await transaction.CommitAsync(ct);
    }
}