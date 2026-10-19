using Dapper;
using Microsoft.Extensions.Options;
using SmartPlanif.Application.Abstractions;

namespace SmartPlanif.Infrastructure.MySql;

/// <summary>Lecture des lignes (drligne + drtyli + drdeleg), de leurs itinéraires (dritin + drstati) et de leurs voyages (trips). Aucune écriture.</summary>
public sealed class MySqlLineRepository(IDbConnectionFactory db, IOptions<TripSourceOptions> options) : ILineRepository
{
    private sealed class LineRow
    {
        public long Id { get; set; }
        public string? NameFr { get; set; }
        public string? NameAr { get; set; }
        public long? TypeId { get; set; }
        public string? TypeLabel { get; set; }
        public string? TypeLabelAr { get; set; }
        public double? Km { get; set; }
        public long? CenterId { get; set; }
        public long? DelegationId { get; set; }
        public string? DelegationLabel { get; set; }
        public long? Nature { get; set; }
        public string? TariffType { get; set; }
        public long ActiveFlag { get; set; }
        public long Sae { get; set; }
        public long Stops { get; set; }
    }

    private sealed class StatsRow
    {
        public long LineId { get; set; }
        public long Trips { get; set; }
        public long Planned { get; set; }
        public long Realized { get; set; }
    }

    private sealed class StopDb
    {
        public long Sequence { get; set; }
        public long? StationId { get; set; }
        public string? NameFr { get; set; }
        public string? NameAr { get; set; }
        public long? Km { get; set; }
        public long? Minutes { get; set; }
        public double? Lat { get; set; }
        public double? Lon { get; set; }
        public long? Section { get; set; }
    }

    private sealed class TripDb
    {
        public long TripId { get; set; }
        public DateTime Departure { get; set; }
        public DateTime? Arrival { get; set; }
        public long Direction { get; set; }
        public string? BusPlate { get; set; }
        public string? DriverName { get; set; }
        public string? ReceiverName { get; set; }
        public string? RealBusPlate { get; set; }
        public string? RealDriverName { get; set; }
        public string? RealReceiverName { get; set; }
        public long HasReal { get; set; }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LineListItem>> GetLinesAsync(CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        var rows = await connection.QueryAsync<LineRow>(new CommandDefinition("""
            SELECT l.DENUMLI AS Id, l.DENOMLI AS NameFr, l.DENOMLA AS NameAr,
                   l.DECTYLI AS TypeId, ty.DELTYLI AS TypeLabel, ty.DELTYLA AS TypeLabelAr,
                   l.DENBRKM AS Km, l.DECCENT AS CenterId, l.DECDELEG AS DelegationId, d.LIBDELEGFR AS DelegationLabel,
                   l.DENATLI AS Nature, l.DECTYTA AS TariffType,
                   CASE WHEN l.DEACTIF = 1 THEN 1 ELSE 0 END AS ActiveFlag,
                   CASE WHEN l.SAE = 1 THEN 1 ELSE 0 END AS Sae,
                   (SELECT COUNT(*) FROM dritin i WHERE i.DENUMLI = l.DENUMLI) AS Stops
            FROM drligne l
            LEFT JOIN drtyli ty ON ty.DECTYLI = l.DECTYLI
            LEFT JOIN drdeleg d ON d.DECDELEG = l.DECDELEG
            ORDER BY l.DENUMLI
            """, cancellationToken: ct));

        return rows.Select(r => new LineListItem(
            r.Id, r.NameFr, r.NameAr, r.TypeId, r.TypeLabel, r.TypeLabelAr, r.Km, r.CenterId, r.DelegationId,
            r.DelegationLabel, r.Nature, r.TariffType, r.ActiveFlag == 1, r.Sae == 1, (int)r.Stops)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LineDayStats>> GetDayStatsAsync(DateOnly date, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        var day = TripSql.Day(TripDayMapper.Map(date, options.Value.TemplateYear));

        var planned = $"{TripSql.Has("t.BUS_PR")} AND {TripSql.Has("t.CHAUFF_PR")} AND {TripSql.Has("t.REC_PR")}";
        var realized = $"{TripSql.Has("t.BUS_RE")} AND {TripSql.Has("t.CHAUFF_RE")} AND {TripSql.Has("t.REC_RE")}";

        var rows = await connection.QueryAsync<StatsRow>(new CommandDefinition($"""
            SELECT t.DENUMLI AS LineId, COUNT(*) AS Trips,
                   CAST(COALESCE(SUM(CASE WHEN {planned} THEN 1 ELSE 0 END), 0) AS SIGNED) AS Planned,
                   CAST(COALESCE(SUM(CASE WHEN {realized} THEN 1 ELSE 0 END), 0) AS SIGNED) AS Realized
            FROM trips t
            WHERE {TripSql.DayFilter} AND t.TIME_DEPART IS NOT NULL
            GROUP BY t.DENUMLI
            """, day, cancellationToken: ct));

        return rows.Select(r => new LineDayStats(r.LineId, (int)r.Trips, (int)r.Planned, (int)r.Realized)).ToList();
    }

    private static async Task<List<StopDb>> ReadStopsAsync(System.Data.Common.DbConnection connection, long lineId, CancellationToken ct) =>
        (await connection.QueryAsync<StopDb>(new CommandDefinition("""
            SELECT i.DENUMLG AS Sequence, i.DECSTAT AS StationId, s.DELSTAT AS NameFr, s.DELSTAA AS NameAr,
                   i.DEKMSTA AS Km, i.DEDURTR AS Minutes, s.STOP_LAT AS Lat, s.STOP_LON AS Lon, i.DESECTI AS Section
            FROM dritin i
            LEFT JOIN drstati s ON s.DECSTAT = i.DECSTAT
            WHERE i.DENUMLI = @LineId
            ORDER BY i.DENUMLG
            """, new { LineId = lineId }, cancellationToken: ct))).ToList();

    private ItineraryDurationResult Resolve(IEnumerable<StopDb> stops) =>
        ItineraryDuration.Resolve(stops.Select(s => new ItineraryStop(s.Sequence, s.Minutes, s.Km)).ToList(),
                                  options.Value.ItineraryDuration);

    /// <inheritdoc />
    public async Task<LineItinerary> GetItineraryAsync(long lineId, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        var stops = await ReadStopsAsync(connection, lineId, ct);
        var resolved = Resolve(stops);
        return new LineItinerary(
            stops.Select(s => new LineStopRow(s.Sequence, s.StationId, s.NameFr, s.NameAr, s.Km, s.Minutes, s.Lat, s.Lon, s.Section)).ToList(),
            resolved.Duration?.TotalMinutes, resolved.Method, resolved.SpeedKmh);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LineTripRow>> GetLineTripsAsync(long lineId, DateOnly date, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        var tableDay = TripDayMapper.Map(date, options.Value.TemplateYear).ToDateTime(TimeOnly.MinValue);

        var rows = (await connection.QueryAsync<TripDb>(new CommandDefinition($"""
            SELECT t.ID AS TripId, t.TIME_DEPART AS Departure, {TripSql.ArrivalSubquery} AS Arrival,
                   COALESCE(t.DIRECTION_ID, 0) AS Direction,
                   v.DEMATRI AS BusPlate, a.DENAGEN AS DriverName, r.DENAGEN AS ReceiverName,
                   vr.DEMATRI AS RealBusPlate, ar.DENAGEN AS RealDriverName, rr.DENAGEN AS RealReceiverName,
                   CASE WHEN COALESCE(t.BUS_RE, 0) > 0 OR COALESCE(t.CHAUFF_RE, 0) > 0 OR COALESCE(t.REC_RE, 0) > 0 THEN 1 ELSE 0 END AS HasReal
            FROM trips t
            LEFT JOIN drvehic v ON v.DECODVH = t.BUS_PR
            LEFT JOIN dragent a ON a.DECAGEN = t.CHAUFF_PR
            LEFT JOIN dragent r ON r.DECAGEN = t.REC_PR
            LEFT JOIN drvehic vr ON vr.DECODVH = t.BUS_RE
            LEFT JOIN dragent ar ON ar.DECAGEN = t.CHAUFF_RE
            LEFT JOIN dragent rr ON rr.DECAGEN = t.REC_RE
            WHERE {TripSql.DayFilter} AND t.TIME_DEPART IS NOT NULL AND t.DENUMLI = @LineId
            ORDER BY t.TIME_DEPART, t.ID
            """, new { From = tableDay, ToExclusive = tableDay.AddDays(1), LineId = lineId }, cancellationToken: ct))).ToList();

        // Voyages futurs : pas de stop_times, l'arrivée est déduite de la durée de l'itinéraire (comme pour le moteur).
        TimeSpan? fallback = null;
        if (rows.Any(r => r.Arrival is null))
            fallback = Resolve(await ReadStopsAsync(connection, lineId, ct)).Duration;

        return rows.Select(r =>
        {
            var arrivalRaw = r.Arrival ?? (fallback is { } d ? r.Departure + d : (DateTime?)null);
            var times = TripTimeNormalizer.Normalize(date.ToDateTime(TimeOnly.MinValue), r.Departure, arrivalRaw);
            return new LineTripRow(
                r.TripId, times?.Departure ?? r.Departure, times?.Arrival ?? arrivalRaw, (int)r.Direction,
                r.BusPlate, r.DriverName, r.ReceiverName, r.RealBusPlate, r.RealDriverName, r.RealReceiverName, r.HasReal == 1);
        }).ToList();
    }
}