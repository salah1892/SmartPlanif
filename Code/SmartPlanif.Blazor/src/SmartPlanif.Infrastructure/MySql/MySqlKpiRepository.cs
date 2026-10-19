using Dapper;
using Microsoft.Extensions.Options;
using SmartPlanif.Application.Abstractions;

namespace SmartPlanif.Infrastructure.MySql;

/// <summary>Agrégations SQL pour le tableau de bord. Lecture seule, une requête par vue.</summary>
public sealed class MySqlKpiRepository(IDbConnectionFactory db, IOptions<TripSourceOptions> options) : IKpiRepository
{
    private sealed class CoverageRow
    {
        public DateTime Day { get; set; }
        public long Trips { get; set; }
        public long Complete { get; set; }
        public long AnyPlan { get; set; }
        public long Realized { get; set; }
    }

    private sealed class StatsRow
    {
        public string? Label { get; set; }
        public long Trips { get; set; } public long WithPlan { get; set; } public long WithReal { get; set; } public long PlanAndReal { get; set; }
        public long BusPlanned { get; set; } public long BusNoReal { get; set; } public long BusChanged { get; set; }
        public long DriverPlanned { get; set; } public long DriverNoReal { get; set; } public long DriverChanged { get; set; }
        public long ReceiverPlanned { get; set; } public long ReceiverNoReal { get; set; } public long ReceiverChanged { get; set; }
        public long AnyChanged { get; set; } public long ChangeFlag { get; set; }
        public long DelayKnown { get; set; } public long OnTime { get; set; } public double? AvgDelay { get; set; }
    }

    private string LineJoin => "JOIN drligne l ON l.DENUMLI = t.DENUMLI" + (options.Value.RequireActiveLine ? " AND l.DEACTIF = 1" : "");

    /// <summary>Plage demandée -> plage lue dans trips (année modèle éventuelle) : début converti + nombre de jours.</summary>
    private (DateTime MappedFrom, int Days) MapRange(DateOnly from, DateOnly to) =>
        (TripDayMapper.Map(from, options.Value.TemplateYear).ToDateTime(TimeOnly.MinValue), to.DayNumber - from.DayNumber + 1);

    private static string S(string condition, string alias) => $"CAST(COALESCE(SUM({condition}), 0) AS SIGNED) AS {alias}";

    /// <summary>Colonnes d'agrégation prévu vs réel. « Renseigné » = identifiant strictement positif (NULL et 0 = aucune ressource).</summary>
    private static string StatsColumns()
    {
        static string Has(string c) => TripSql.Has("t." + c);
        var anyPr = $"({Has("BUS_PR")} OR {Has("CHAUFF_PR")} OR {Has("REC_PR")})";
        var anyRe = $"({Has("BUS_RE")} OR {Has("CHAUFF_RE")} OR {Has("REC_RE")})";
        static string Changed(string pr, string re) => $"({Has(pr)} AND {Has(re)} AND t.{re} <> t.{pr})";
        static string NoReal(string pr, string re) => $"({Has(pr)} AND NOT ({Has(re)}))";
        var anyChanged = $"({Changed("BUS_PR", "BUS_RE")} OR {Changed("CHAUFF_PR", "CHAUFF_RE")} OR {Changed("REC_PR", "REC_RE")})";
        const string delay = "t.TIME_DEPART_R IS NOT NULL AND t.AVANCE_RETARD IS NOT NULL";

        return string.Join(", ", new[]
        {
            "COUNT(*) AS Trips",
            S(anyPr, "WithPlan"), S(anyRe, "WithReal"), S($"{anyPr} AND {anyRe}", "PlanAndReal"),
            S(Has("BUS_PR"), "BusPlanned"), S(NoReal("BUS_PR", "BUS_RE"), "BusNoReal"), S(Changed("BUS_PR", "BUS_RE"), "BusChanged"),
            S(Has("CHAUFF_PR"), "DriverPlanned"), S(NoReal("CHAUFF_PR", "CHAUFF_RE"), "DriverNoReal"), S(Changed("CHAUFF_PR", "CHAUFF_RE"), "DriverChanged"),
            S(Has("REC_PR"), "ReceiverPlanned"), S(NoReal("REC_PR", "REC_RE"), "ReceiverNoReal"), S(Changed("REC_PR", "REC_RE"), "ReceiverChanged"),
            S(anyChanged, "AnyChanged"),
            S("COALESCE(t.CHANGEMENT, 0) > 0", "ChangeFlag"),
            S(delay, "DelayKnown"),
            S($"{delay} AND t.AVANCE_RETARD <= @OnTime", "OnTime"),
            $"CAST(AVG(CASE WHEN {delay} THEN t.AVANCE_RETARD END) AS DOUBLE) AS AvgDelay",
        });
    }

    private static PlanVsRealStats ToStats(string label, StatsRow r) => new(
        label, r.Trips, r.WithPlan, r.WithReal, r.PlanAndReal,
        new RoleChange(r.BusPlanned, r.BusChanged, r.BusNoReal),
        new RoleChange(r.DriverPlanned, r.DriverChanged, r.DriverNoReal),
        new RoleChange(r.ReceiverPlanned, r.ReceiverChanged, r.ReceiverNoReal),
        r.AnyChanged, r.ChangeFlag, r.DelayKnown, r.AvgDelay, r.OnTime);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DayCoverage>> GetCoverageAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var (mappedFrom, days) = MapRange(from, to);
        await using var connection = await db.OpenAsync(ct);
        var rows = await connection.QueryAsync<CoverageRow>(new CommandDefinition($"""
            SELECT COALESCE(DATE(t.DEDATED), DATE(t.TIME_DEPART)) AS Day,
                   COUNT(*) AS Trips,
                   {S($"{TripSql.Has("t.BUS_PR")} AND {TripSql.Has("t.CHAUFF_PR")} AND {TripSql.Has("t.REC_PR")}", "Complete")},
                   {S($"{TripSql.Has("t.BUS_PR")} OR {TripSql.Has("t.CHAUFF_PR")} OR {TripSql.Has("t.REC_PR")}", "AnyPlan")},
                   {S($"{TripSql.Has("t.BUS_RE")} AND {TripSql.Has("t.CHAUFF_RE")} AND {TripSql.Has("t.REC_RE")}", "Realized")}
            FROM trips t {LineJoin}
            WHERE {TripSql.DayFilter} AND t.TIME_DEPART IS NOT NULL
            GROUP BY 1 ORDER BY 1
            """, new { From = mappedFrom, ToExclusive = mappedFrom.AddDays(days) }, cancellationToken: ct));

        // Les jours de la table sont ramenés sur les jours demandés (même décalage que l'année modèle).
        return rows.Select(r => new DayCoverage(
            from.AddDays(DateOnly.FromDateTime(r.Day).DayNumber - DateOnly.FromDateTime(mappedFrom).DayNumber),
            (int)r.Trips, (int)r.Complete, (int)(r.AnyPlan - r.Complete), (int)r.Realized)).ToList();
    }

    /// <inheritdoc />
    public async Task<PlanVsRealStats> GetPlanVsRealAsync(DateOnly from, DateOnly to, int onTimeSeconds, CancellationToken ct)
    {
        var (mappedFrom, days) = MapRange(from, to);
        await using var connection = await db.OpenAsync(ct);
        var row = (await connection.QueryAsync<StatsRow>(new CommandDefinition(
            $"SELECT {StatsColumns()} FROM trips t {LineJoin} WHERE {TripSql.DayFilter} AND t.TIME_DEPART IS NOT NULL",
            new { From = mappedFrom, ToExclusive = mappedFrom.AddDays(days), OnTime = onTimeSeconds }, cancellationToken: ct))).First();
        return ToStats($"{from:dd/MM/yyyy} → {to:dd/MM/yyyy}", row);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlanVsRealStats>> GetPlanVsRealByLineAsync(DateOnly from, DateOnly to, int onTimeSeconds, CancellationToken ct)
    {
        var (mappedFrom, days) = MapRange(from, to);
        await using var connection = await db.OpenAsync(ct);
        var rows = await connection.QueryAsync<StatsRow>(new CommandDefinition($"""
            SELECT COALESCE(MAX(l.DENOMLI), CONCAT('Ligne ', t.DENUMLI)) AS Label, {StatsColumns()}
            FROM trips t {LineJoin}
            WHERE {TripSql.DayFilter} AND t.TIME_DEPART IS NOT NULL
            GROUP BY t.DENUMLI ORDER BY AnyChanged DESC, Trips DESC LIMIT 50
            """, new { From = mappedFrom, ToExclusive = mappedFrom.AddDays(days), OnTime = onTimeSeconds }, cancellationToken: ct));
        return rows.Select(r => ToStats(r.Label ?? "?", r)).ToList();
    }

    // ======================================================================== Historique & qualité
    private const string DayExpr = "COALESCE(DATE(t.DEDATED), DATE(t.TIME_DEPART))";
    private const string EffBus = "COALESCE(NULLIF(t.BUS_RE, 0), NULLIF(t.BUS_PR, 0))";
    private const string EffDriver = "COALESCE(NULLIF(t.CHAUFF_RE, 0), NULLIF(t.CHAUFF_PR, 0))";
    private const string EffReceiver = "COALESCE(NULLIF(t.REC_RE, 0), NULLIF(t.REC_PR, 0))";

    private sealed class DailyRow
    {
        public DateTime Day { get; set; }
        public long Trips { get; set; }
        public long Planned { get; set; }
        public long Realized { get; set; }
        public long BusesUsed { get; set; }
        public long DelayKnown { get; set; }
        public long OnTime { get; set; }
        public double? AvgDelay { get; set; }
        public long PlanAndReal { get; set; }
        public long Changed { get; set; }
    }

    private sealed class AgentDayRow { public DateTime Day { get; set; } public long AgentsUsed { get; set; } }
    private sealed class UsageRow { public long Id { get; set; } public string? Label { get; set; } public long Trips { get; set; } public long DaysUsed { get; set; } }
    private sealed class ResourceDayRow { public DateTime Day { get; set; } public long Trips { get; set; } }

    /// <summary>Jour lu dans la table -> jour demandé (même décalage que l'année modèle).</summary>
    private static DateOnly Unmap(DateTime day, DateTime mappedFrom, DateOnly from) =>
        from.AddDays(DateOnly.FromDateTime(day).DayNumber - DateOnly.FromDateTime(mappedFrom).DayNumber);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DayQuality>> GetDailyQualityAsync(DateOnly from, DateOnly to, int onTimeSeconds, CancellationToken ct)
    {
        var (mappedFrom, days) = MapRange(from, to);
        var parameters = new { From = mappedFrom, ToExclusive = mappedFrom.AddDays(days), OnTime = onTimeSeconds };

        static string Has(string c) => TripSql.Has("t." + c);
        static string Changed(string pr, string re) => $"({Has(pr)} AND {Has(re)} AND t.{re} <> t.{pr})";
        var plannedFull = $"({Has("BUS_PR")} AND {Has("CHAUFF_PR")} AND {Has("REC_PR")})";
        var realFull = $"({Has("BUS_RE")} AND {Has("CHAUFF_RE")} AND {Has("REC_RE")})";
        var anyPr = $"({Has("BUS_PR")} OR {Has("CHAUFF_PR")} OR {Has("REC_PR")})";
        var anyRe = $"({Has("BUS_RE")} OR {Has("CHAUFF_RE")} OR {Has("REC_RE")})";
        var anyChanged = $"({Changed("BUS_PR", "BUS_RE")} OR {Changed("CHAUFF_PR", "CHAUFF_RE")} OR {Changed("REC_PR", "REC_RE")})";
        const string delay = "t.TIME_DEPART_R IS NOT NULL AND t.AVANCE_RETARD IS NOT NULL";

        var sql = $"""
            SELECT {DayExpr} AS Day, COUNT(*) AS Trips,
                   {S($"({plannedFull} OR {realFull})", "Planned")},
                   {S(realFull, "Realized")},
                   CAST(COUNT(DISTINCT {EffBus}) AS SIGNED) AS BusesUsed,
                   {S(delay, "DelayKnown")},
                   {S($"{delay} AND t.AVANCE_RETARD <= @OnTime", "OnTime")},
                   CAST(AVG(CASE WHEN {delay} THEN t.AVANCE_RETARD END) AS DOUBLE) AS AvgDelay,
                   {S($"{anyPr} AND {anyRe}", "PlanAndReal")},
                   {S(anyChanged, "Changed")}
            FROM trips t {LineJoin}
            WHERE {TripSql.DayFilter} AND t.TIME_DEPART IS NOT NULL
            GROUP BY 1 ORDER BY 1
            """;

        // Agents distincts par jour : chauffeurs et receveurs réunis (UNION ALL puis COUNT DISTINCT).
        var agentSql = $"""
            SELECT x.Day AS Day, CAST(COUNT(DISTINCT x.AgentId) AS SIGNED) AS AgentsUsed
            FROM (
                SELECT {DayExpr} AS Day, {EffDriver} AS AgentId FROM trips t {LineJoin}
                WHERE {TripSql.DayFilter} AND t.TIME_DEPART IS NOT NULL
                UNION ALL
                SELECT {DayExpr} AS Day, {EffReceiver} AS AgentId FROM trips t {LineJoin}
                WHERE {TripSql.DayFilter} AND t.TIME_DEPART IS NOT NULL
            ) x
            WHERE x.AgentId IS NOT NULL
            GROUP BY x.Day
            """;

        await using var connection = await db.OpenAsync(ct);
        var rows = (await connection.QueryAsync<DailyRow>(new CommandDefinition(sql, parameters, cancellationToken: ct))).ToList();
        var agents = (await connection.QueryAsync<AgentDayRow>(new CommandDefinition(agentSql, parameters, cancellationToken: ct)))
            .ToDictionary(r => r.Day.Date, r => r.AgentsUsed);

        return rows.Select(r => new DayQuality(
            Unmap(r.Day, mappedFrom, from), (int)r.Trips, (int)r.Planned, (int)r.Realized, (int)r.BusesUsed,
            (int)agents.GetValueOrDefault(r.Day.Date), r.DelayKnown, r.OnTime, r.AvgDelay, r.PlanAndReal, r.Changed)).ToList();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ResourceUsage>> GetBusUsageAsync(DateOnly from, DateOnly to, CancellationToken ct) =>
        UsageAsync(true, from, to, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<ResourceUsage>> GetAgentUsageAsync(DateOnly from, DateOnly to, CancellationToken ct) =>
        UsageAsync(false, from, to, ct);

    /// <summary>Voyages et jours d'activité de CHAQUE bus / agent (les ressources jamais utilisées apparaissent avec 0).</summary>
    private async Task<IReadOnlyList<ResourceUsage>> UsageAsync(bool bus, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var (mappedFrom, days) = MapRange(from, to);
        var common = $"FROM trips t {LineJoin} WHERE {TripSql.DayFilter} AND t.TIME_DEPART IS NOT NULL";
        var sql = bus
            ? $"""
               SELECT v.DECODVH AS Id, COALESCE(v.DEMATRI, CONCAT('#', v.DECODVH)) AS Label,
                      CAST(COUNT(x.Day) AS SIGNED) AS Trips, CAST(COUNT(DISTINCT x.Day) AS SIGNED) AS DaysUsed
               FROM drvehic v
               LEFT JOIN (SELECT {EffBus} AS ResId, {DayExpr} AS Day {common}) x ON x.ResId = v.DECODVH
               GROUP BY v.DECODVH, v.DEMATRI
               ORDER BY Trips DESC, v.DECODVH
               """
            : $"""
               SELECT a.DECAGEN AS Id, COALESCE(a.DENAGEN, CONCAT('#', a.DECAGEN)) AS Label,
                      CAST(COUNT(x.Day) AS SIGNED) AS Trips, CAST(COUNT(DISTINCT x.Day) AS SIGNED) AS DaysUsed
               FROM dragent a
               LEFT JOIN (
                   SELECT {EffDriver} AS ResId, {DayExpr} AS Day {common}
                   UNION ALL
                   SELECT {EffReceiver} AS ResId, {DayExpr} AS Day {common}
               ) x ON x.ResId = a.DECAGEN
               GROUP BY a.DECAGEN, a.DENAGEN
               ORDER BY Trips DESC, a.DECAGEN
               """;

        await using var connection = await db.OpenAsync(ct);
        var rows = await connection.QueryAsync<UsageRow>(new CommandDefinition(
            sql, new { From = mappedFrom, ToExclusive = mappedFrom.AddDays(days) }, cancellationToken: ct));
        return rows.Select(r => new ResourceUsage(r.Id, r.Label ?? $"#{r.Id}", (int)r.Trips, (int)r.DaysUsed)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ResourceDay>> GetResourceDailyAsync(bool isBus, long id, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var (mappedFrom, days) = MapRange(from, to);
        var condition = isBus ? $"{EffBus} = @Id" : $"({EffDriver} = @Id OR {EffReceiver} = @Id)";
        await using var connection = await db.OpenAsync(ct);
        var rows = await connection.QueryAsync<ResourceDayRow>(new CommandDefinition($"""
            SELECT {DayExpr} AS Day, COUNT(*) AS Trips
            FROM trips t {LineJoin}
            WHERE {TripSql.DayFilter} AND t.TIME_DEPART IS NOT NULL AND {condition}
            GROUP BY 1 ORDER BY 1
            """, new { From = mappedFrom, ToExclusive = mappedFrom.AddDays(days), Id = id }, cancellationToken: ct));
        return rows.Select(r => new ResourceDay(Unmap(r.Day, mappedFrom, from), (int)r.Trips)).ToList();
    }
}