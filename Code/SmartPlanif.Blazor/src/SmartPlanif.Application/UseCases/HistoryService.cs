using System.Globalization;
using SmartPlanif.Application.Abstractions;
using SmartPlanif.Domain.Allocation;
using SmartPlanif.Domain.Entities;

namespace SmartPlanif.Application.UseCases;

/// <summary>Regroupement des points de la courbe : Auto choisit selon la longueur de la période.</summary>
public enum HistoryGranularity { Auto, Day, Week, Month }

/// <summary>Indicateurs de qualité d'un point (jour, semaine ou mois). Les taux sont en pourcentage (0 à 100).</summary>
public sealed record QualityPoint(
    string Label, DateOnly Start, int DaysWithTrips, int Trips,
    double? CoverageRate, double? RealizedRate, double? BusUtilization, double? AgentUtilization,
    double? OnTimeRate, double? AvgDelayMinutes, double? ChangeRate);

/// <summary>Rapport complet de l'onglet « Historique & qualité ».</summary>
public sealed record HistoryReport(
    DateOnly From, DateOnly To, HistoryGranularity Granularity, int ActiveBuses, int PlannableAgents,
    IReadOnlyList<DayQuality> Days, IReadOnlyList<QualityPoint> Points, QualityPoint Total,
    IReadOnlyList<ResourceUsage> Buses, IReadOnlyList<ResourceUsage> Agents,
    DistributionStats BusDistribution, DistributionStats AgentDistribution);

/// <summary>Calculs purs (testables sans base).</summary>
public static class HistoryMath
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static HistoryGranularity Resolve(DateOnly from, DateOnly to, HistoryGranularity requested)
    {
        if (requested != HistoryGranularity.Auto) return requested;
        var days = to.DayNumber - from.DayNumber + 1;
        return days <= 45 ? HistoryGranularity.Day : days <= 200 ? HistoryGranularity.Week : HistoryGranularity.Month;
    }

    /// <summary>Début de la période qui contient le jour (lundi pour une semaine, 1er pour un mois).</summary>
    public static DateOnly KeyOf(DateOnly day, HistoryGranularity g) => g switch
    {
        HistoryGranularity.Week => day.AddDays(-(((int)day.DayOfWeek + 6) % 7)),
        HistoryGranularity.Month => new DateOnly(day.Year, day.Month, 1),
        _ => day,
    };

    public static string LabelOf(DateOnly key, HistoryGranularity g) => g switch
    {
        HistoryGranularity.Week => "S " + key.ToString("dd/MM/yy", Inv),
        HistoryGranularity.Month => key.ToString("MM/yyyy", Inv),
        _ => key.ToString("dd/MM/yy", Inv),
    };

    /// <summary>Agrège les jours (ayant des voyages) en points de la granularité demandée.</summary>
    public static IReadOnlyList<QualityPoint> Aggregate(
        IEnumerable<DayQuality> days, int activeBuses, int plannableAgents, HistoryGranularity g) =>
        days.Where(d => d.Trips > 0)
            .GroupBy(d => KeyOf(d.Day, g))
            .OrderBy(grp => grp.Key)
            .Select(grp => Point(LabelOf(grp.Key, g), grp.Key, grp.ToList(), activeBuses, plannableAgents))
            .ToList();

    public static QualityPoint Point(string label, DateOnly start, IReadOnlyList<DayQuality> d, int activeBuses, int plannableAgents)
    {
        double trips = d.Sum(x => (double)x.Trips);
        double delayKnown = d.Sum(x => (double)x.DelayKnown);
        double planAndReal = d.Sum(x => (double)x.PlanAndReal);

        static double? Rate(double num, double den) => den > 0 ? num / den * 100 : null;

        double? avgDelay = delayKnown > 0
            ? d.Where(x => x.AvgDelaySeconds is not null).Sum(x => x.AvgDelaySeconds!.Value * x.DelayKnown) / delayKnown / 60
            : null;

        return new QualityPoint(
            label, start, d.Count, (int)trips,
            Rate(d.Sum(x => (double)x.Planned), trips),
            Rate(d.Sum(x => (double)x.Realized), trips),
            d.Count > 0 && activeBuses > 0 ? d.Average(x => Math.Min(100, x.BusesUsed * 100.0 / activeBuses)) : null,
            d.Count > 0 && plannableAgents > 0 ? d.Average(x => Math.Min(100, x.AgentsUsed * 100.0 / plannableAgents)) : null,
            Rate(d.Sum(x => (double)x.OnTime), delayKnown),
            avgDelay,
            Rate(d.Sum(x => (double)x.Changed), planAndReal));
    }

    /// <summary>Série « voyages par période » d'une ressource, jours sans voyage compris (valeur 0).</summary>
    public static (IReadOnlyList<string> Labels, IReadOnlyList<double?> Values) ResourceSeries(
        IEnumerable<ResourceDay> rows, DateOnly from, DateOnly to, HistoryGranularity g)
    {
        var byDay = rows.GroupBy(r => r.Day).ToDictionary(x => x.Key, x => x.Sum(r => r.Trips));
        var buckets = new SortedDictionary<DateOnly, int>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            var key = KeyOf(d, g);
            buckets.TryGetValue(key, out var current);
            byDay.TryGetValue(d, out var trips);
            buckets[key] = current + trips;
        }
        return (buckets.Keys.Select(k => LabelOf(k, g)).ToList(), buckets.Values.Select(v => (double?)v).ToList());
    }
}

/// <summary>Cas d'usage « historique de qualité et d'utilisation des ressources » (lecture seule, jusqu'à hier).</summary>
public sealed class HistoryService(IKpiRepository kpi, IResourceRepository resources, PlanningCalendar calendar)
{
    /// <summary>Dernier jour pris en compte : hier (selon l'horloge de planification).</summary>
    public DateOnly Yesterday => calendar.Today.AddDays(-1);

    public async Task<HistoryReport> GetAsync(
        DateOnly from, DateOnly to, int onTimeSeconds, HistoryGranularity granularity, CancellationToken ct = default)
    {
        if (to > Yesterday) to = Yesterday;
        if (from > to) from = to;

        var days = (await kpi.GetDailyQualityAsync(from, to, onTimeSeconds, ct))
            .Where(d => d.Trips > 0).OrderBy(d => d.Day).ToList();
        var effectiveFrom = days.Count > 0 ? days[0].Day : from;   // « Tout » : on commence au premier jour réel

        var pool = await resources.GetPoolAsync(effectiveFrom, to, ct);
        var activeBusIds = pool.Buses.Where(b => b.Active).Select(b => b.Id).ToHashSet();
        var agentIds = pool.Agents.Where(a => a.Role.IsPlannable()).Select(a => a.Id).ToHashSet();

        var buses = await kpi.GetBusUsageAsync(effectiveFrom, to, ct);
        var agents = await kpi.GetAgentUsageAsync(effectiveFrom, to, ct);

        var resolved = HistoryMath.Resolve(effectiveFrom, to, granularity);
        var points = HistoryMath.Aggregate(days, activeBusIds.Count, agentIds.Count, resolved);
        var total = HistoryMath.Point("Total", effectiveFrom, days, activeBusIds.Count, agentIds.Count);

        var busDistribution = Distribution.Describe("Bus : voyages sur la période", "voyages",
            buses.Where(b => activeBusIds.Contains(b.Id)).Select(b => (double)b.Trips).ToList());
        var agentDistribution = Distribution.Describe("Agents : postes tenus sur la période", "postes",
            agents.Where(a => agentIds.Contains(a.Id)).Select(a => (double)a.Trips).ToList());

        return new HistoryReport(effectiveFrom, to, resolved, activeBusIds.Count, agentIds.Count,
            days, points, total, buses, agents, busDistribution, agentDistribution);
    }

    /// <summary>Voyages par jour d'un bus ou d'un agent (pour le graphique de détail).</summary>
    public Task<IReadOnlyList<ResourceDay>> GetResourceDailyAsync(
        bool isBus, long id, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        kpi.GetResourceDailyAsync(isBus, id, from, to, ct);
}
