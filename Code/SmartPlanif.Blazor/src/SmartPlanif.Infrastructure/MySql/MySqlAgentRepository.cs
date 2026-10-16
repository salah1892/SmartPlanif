namespace SmartPlanif.Infrastructure.MySql;

using Dapper;
using Microsoft.Extensions.Options;
using SmartPlanif.Application.Abstractions;


/// <summary>Lecture des agents (dragent + drdeleg) et de leurs voyages (trips). Aucune écriture.</summary>
public sealed class MySqlAgentRepository(IDbConnectionFactory db, IOptions<TripSourceOptions> options) : IAgentRepository
{
    private sealed class AgentRow
    {
        public long Id { get; set; }
        public string? NameFr { get; set; }
        public string? NameAr { get; set; }
        public string? Qual { get; set; }
        public long? RestDay { get; set; }
        public long? CenterId { get; set; }
        public long? DelegationId { get; set; }
        public string? DelegationLabel { get; set; }
        public long? Pda { get; set; }
    }

    private sealed class TripRow
    {
        public long TripId { get; set; }
        public string LineLabel { get; set; } = "";
        public DateTime Departure { get; set; }
        public DateTime? Arrival { get; set; }
        public long PlannedPost { get; set; }
        public long RealPost { get; set; }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgentListItem>> GetAgentsAsync(CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        var rows = await connection.QueryAsync<AgentRow>(new CommandDefinition("""
            SELECT a.DECAGEN AS Id, a.DENAGEN AS NameFr, a.DENAGEA AS NameAr, a.DECQUAL AS Qual,
                   a.DECJOUR AS RestDay, a.DECCENT AS CenterId, a.DECDELEG AS DelegationId,
                   d.LIBDELEGFR AS DelegationLabel, a.PDA AS Pda
            FROM dragent a
            LEFT JOIN drdeleg d ON d.DECDELEG = a.DECDELEG
            ORDER BY a.DENAGEN, a.DECAGEN
            """, cancellationToken: ct));

        return rows.Select(r => new AgentListItem(
            r.Id, r.NameFr, r.NameAr, r.Qual, r.RestDay is >= 1 and <= 7 ? (int)r.RestDay : null,
            r.CenterId, r.DelegationId, r.DelegationLabel, r.Pda)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgentTripRow>> GetAgentTripsAsync(long agentId, DateOnly date, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        var tableDay = TripDayMapper.Map(date, options.Value.TemplateYear);
        var from = tableDay.ToDateTime(TimeOnly.MinValue);

        var rows = await connection.QueryAsync<TripRow>(new CommandDefinition($"""
            SELECT t.ID AS TripId, COALESCE(l.DENOMLI, CONCAT('Ligne ', t.DENUMLI)) AS LineLabel,
                   t.TIME_DEPART AS Departure, {TripSql.ArrivalSubquery} AS Arrival,
                   CASE WHEN t.CHAUFF_PR = @AgentId THEN 1 WHEN t.REC_PR = @AgentId THEN 2 ELSE 0 END AS PlannedPost,
                   CASE WHEN t.CHAUFF_RE = @AgentId THEN 1 WHEN t.REC_RE = @AgentId THEN 2 ELSE 0 END AS RealPost
            FROM trips t
            LEFT JOIN drligne l ON l.DENUMLI = t.DENUMLI
            WHERE {TripSql.DayFilter} AND t.TIME_DEPART IS NOT NULL
              AND (t.CHAUFF_PR = @AgentId OR t.REC_PR = @AgentId OR t.CHAUFF_RE = @AgentId OR t.REC_RE = @AgentId)
            ORDER BY t.TIME_DEPART, t.ID
            """, new { From = from, ToExclusive = from.AddDays(1), AgentId = agentId }, cancellationToken: ct));

        return rows.Select(r =>
        {
            var times = TripTimeNormalizer.Normalize(date.ToDateTime(TimeOnly.MinValue), r.Departure, r.Arrival);
            return new AgentTripRow(r.TripId, r.LineLabel, times?.Departure ?? r.Departure, times?.Arrival ?? r.Arrival,
                (AgentPost)r.PlannedPost, (AgentPost)r.RealPost);
        }).ToList();
    }
}