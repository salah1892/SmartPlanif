using Dapper;
using Microsoft.Extensions.Options;
using SmartPlanif.Application.Abstractions;

namespace SmartPlanif.Infrastructure.MySql;

/// <summary>Congés des agents dans resource_unavailability (KIND = 'AGENT'). Les lignes KIND = 'BUS' ne sont jamais touchées.</summary>
public sealed class MySqlLeaveRepository(IDbConnectionFactory db, IOptions<TripSourceOptions> options) : ILeaveRepository
{
    private sealed class LeaveRow
    {
        public long Id { get; set; }
        public long AgentId { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string? Type { get; set; }
        public string? Reason { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    private sealed class ConflictRow
    {
        public long TripId { get; set; }
        public string LineLabel { get; set; } = "";
        public DateTime Departure { get; set; }
        public DateTime? ServiceDay { get; set; }
        public long Post { get; set; }
    }

    /// <inheritdoc />
    public async Task<bool> IsReadyAsync(CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        return (await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() " +
            "AND table_name = 'resource_unavailability' AND column_name = 'LEAVE_TYPE'", cancellationToken: ct))) > 0;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgentLeave>> GetLeavesAsync(long? agentId, DateTime from, DateTime to, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        var agentFilter = agentId is null ? "" : "AND RESOURCE_ID = @AgentId";
        var rows = await connection.QueryAsync<LeaveRow>(new CommandDefinition($"""
            SELECT ID AS Id, RESOURCE_ID AS AgentId, START_AT AS StartAt, END_AT AS EndAt,
                   COALESCE(LEAVE_TYPE, 'AUTRE') AS Type, REASON AS Reason, CREATED_BY AS CreatedBy, CREATED_AT AS CreatedAt
            FROM resource_unavailability
            WHERE KIND = 'AGENT' AND END_AT > @From AND START_AT < @To {agentFilter}
            ORDER BY START_AT DESC, ID DESC
            """, new { From = from, To = to, AgentId = agentId }, cancellationToken: ct));
        return rows.Select(r => new AgentLeave(r.Id, r.AgentId, r.StartAt, r.EndAt, r.Type ?? "AUTRE", r.Reason, r.CreatedBy, r.CreatedAt)).ToList();
    }

    /// <inheritdoc />
    public async Task<long> AddAsync(AgentLeave leave, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition("""
            INSERT INTO resource_unavailability (KIND, RESOURCE_ID, START_AT, END_AT, REASON, LEAVE_TYPE, CREATED_BY)
            VALUES ('AGENT', @AgentId, @StartAt, @EndAt, @Reason, @Type, @CreatedBy);
            SELECT LAST_INSERT_ID();
            """, leave, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id, DateTime notEndedBefore, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM resource_unavailability WHERE ID = @Id AND KIND = 'AGENT' AND END_AT > @Min",
            new { Id = id, Min = notEndedBefore }, cancellationToken: ct)) > 0;
    }

    /// <inheritdoc />

    /// <inheritdoc />
    public async Task<long> AddBusUnavailabilityAsync(long busId, DateTime startAt, DateTime endAt, string reason, string createdBy, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition("""
        INSERT INTO resource_unavailability (KIND, RESOURCE_ID, START_AT, END_AT, REASON, CREATED_BY)
        VALUES ('BUS', @BusId, @StartAt, @EndAt, @Reason, @CreatedBy);
        SELECT LAST_INSERT_ID();
        """, new { BusId = busId, StartAt = startAt, EndAt = endAt, Reason = reason, CreatedBy = createdBy },
            cancellationToken: ct));
    }
    public async Task<IReadOnlyList<LeaveConflict>> GetConflictsAsync(long agentId, DateOnly firstDay, DateOnly lastDay, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        var mappedFrom = TripDayMapper.Map(firstDay, options.Value.TemplateYear);
        var days = lastDay.DayNumber - firstDay.DayNumber + 1;
        var from = mappedFrom.ToDateTime(TimeOnly.MinValue);

        var rows = await connection.QueryAsync<ConflictRow>(new CommandDefinition($"""
            SELECT t.ID AS TripId, COALESCE(l.DENOMLI, CONCAT('Ligne ', t.DENUMLI)) AS LineLabel,
                   t.TIME_DEPART AS Departure, COALESCE(t.DEDATED, t.TIME_DEPART) AS ServiceDay,
                   CASE WHEN t.CHAUFF_PR = @AgentId THEN 1 ELSE 2 END AS Post
            FROM trips t LEFT JOIN drligne l ON l.DENUMLI = t.DENUMLI
            WHERE {TripSql.DayFilter} AND t.TIME_DEPART IS NOT NULL
              AND (t.CHAUFF_PR = @AgentId OR t.REC_PR = @AgentId) AND {TripSql.NotRealized}
            ORDER BY t.TIME_DEPART, t.ID LIMIT 100
            """, new { From = from, ToExclusive = from.AddDays(days), AgentId = agentId }, cancellationToken: ct));

        // Jour de la table -> jour demandé (année modèle), heure conservée.
        var shift = firstDay.DayNumber - mappedFrom.DayNumber;
        return rows.Select(r => new LeaveConflict(
            r.TripId, r.LineLabel,
            (r.ServiceDay ?? r.Departure).Date.AddDays(shift) + r.Departure.TimeOfDay,
            (AgentPost)r.Post)).ToList();
    }
}