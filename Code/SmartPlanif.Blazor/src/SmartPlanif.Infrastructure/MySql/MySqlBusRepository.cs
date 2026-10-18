using Dapper;
using Microsoft.Extensions.Options;
using SmartPlanif.Application.Abstractions;

namespace SmartPlanif.Infrastructure.MySql;

/// <summary>Lecture des bus (drvehic + drcatve + drdeleg) et de leur usage (trips). Aucune écriture.</summary>
public sealed class MySqlBusRepository(IDbConnectionFactory db, IOptions<TripSourceOptions> options) : IBusRepository
{
    private sealed class BusRow
    {
        public long Id { get; set; }
        public string? Plate { get; set; }
        public long? CategoryId { get; set; }
        public string? CategoryLabel { get; set; }
        public string? CategoryLabelAr { get; set; }
        public long Seats { get; set; }
        public long Standing { get; set; }
        public DateTime? InServiceSince { get; set; }
        public long? CenterId { get; set; }
        public long? DelegationId { get; set; }
        public string? DelegationLabel { get; set; }
        public long? ActiveFlag { get; set; }
    }

    private sealed class UsageRow
    {
        public long BusId { get; set; }
        public long LineId { get; set; }
        public string LineLabel { get; set; } = "";
        public long Trips { get; set; }
    }

    private sealed class TripRow
    {
        public long TripId { get; set; }
        public string LineLabel { get; set; } = "";
        public DateTime Departure { get; set; }
        public DateTime? Arrival { get; set; }
        public long Planned { get; set; }
        public long Real { get; set; }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BusListItem>> GetBusesAsync(CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        // drvehic.DECCENT / DECDELEG sont des DOUBLE : on convertit pour joindre drdeleg (BIGINT).
        var rows = await connection.QueryAsync<BusRow>(new CommandDefinition("""
            SELECT v.DECODVH AS Id, v.DEMATRI AS Plate, v.DECATVH AS CategoryId,
                   c.DECATEG AS CategoryLabel, c.DEACATE AS CategoryLabelAr,
                   COALESCE(v.DENBRPA, 0) AS Seats, COALESCE(v.DENBRPD, 0) AS Standing, v.DEDATEC AS InServiceSince,
                   CAST(v.DECCENT AS SIGNED) AS CenterId, CAST(v.DECDELEG AS SIGNED) AS DelegationId,
                   d.LIBDELEGFR AS DelegationLabel, v.DEACTIF AS ActiveFlag
            FROM drvehic v
            LEFT JOIN drcatve c ON c.DECATVH = v.DECATVH
            LEFT JOIN drdeleg d ON d.DECDELEG = CAST(v.DECDELEG AS SIGNED)
            ORDER BY v.DEMATRI, v.DECODVH
            """, cancellationToken: ct));

        return rows.Select(r => new BusListItem(
            r.Id, r.Plate, r.CategoryId, r.CategoryLabel, r.CategoryLabelAr, (int)r.Seats, (int)r.Standing,
            r.InServiceSince, r.CenterId, r.DelegationId, r.DelegationLabel, r.ActiveFlag)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BusLineUsage>> GetBusLineUsageAsync(DateOnly date, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        var from = TripDayMapper.Map(date, options.Value.TemplateYear).ToDateTime(TimeOnly.MinValue);

        var rows = await connection.QueryAsync<UsageRow>(new CommandDefinition($"""
            SELECT x.BusId, x.LineId, MAX(x.LineLabel) AS LineLabel, COUNT(*) AS Trips
            FROM (
                SELECT COALESCE(NULLIF(t.BUS_RE, 0), NULLIF(t.BUS_PR, 0)) AS BusId, t.DENUMLI AS LineId,
                       COALESCE(l.DENOMLI, CONCAT('Ligne ', t.DENUMLI)) AS LineLabel
                FROM trips t LEFT JOIN drligne l ON l.DENUMLI = t.DENUMLI
                WHERE {TripSql.DayFilter}
            ) x
            WHERE x.BusId IS NOT NULL
            GROUP BY x.BusId, x.LineId
            """, new { From = from, ToExclusive = from.AddDays(1) }, cancellationToken: ct));

        return rows.Select(r => new BusLineUsage(r.BusId, r.LineId, r.LineLabel, (int)r.Trips)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BusTripRow>> GetBusTripsAsync(long busId, DateOnly date, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        var from = TripDayMapper.Map(date, options.Value.TemplateYear).ToDateTime(TimeOnly.MinValue);

        var rows = await connection.QueryAsync<TripRow>(new CommandDefinition($"""
            SELECT t.ID AS TripId, COALESCE(l.DENOMLI, CONCAT('Ligne ', t.DENUMLI)) AS LineLabel,
                   t.TIME_DEPART AS Departure, {TripSql.ArrivalSubquery} AS Arrival,
                   CASE WHEN t.BUS_PR = @BusId THEN 1 ELSE 0 END AS Planned,
                   CASE WHEN t.BUS_RE = @BusId THEN 1 ELSE 0 END AS IsReal
            FROM trips t
            LEFT JOIN drligne l ON l.DENUMLI = t.DENUMLI
            WHERE {TripSql.DayFilter} AND t.TIME_DEPART IS NOT NULL
              AND (t.BUS_PR = @BusId OR t.BUS_RE = @BusId)
            ORDER BY t.TIME_DEPART, t.ID
            """, new { From = from, ToExclusive = from.AddDays(1), BusId = busId }, cancellationToken: ct));

        return rows.Select(r =>
        {
            var times = TripTimeNormalizer.Normalize(date.ToDateTime(TimeOnly.MinValue), r.Departure, r.Arrival);
            return new BusTripRow(r.TripId, r.LineLabel, times?.Departure ?? r.Departure, times?.Arrival ?? r.Arrival,
                r.Planned == 1, r.Real == 1);
        }).ToList();
    }
}