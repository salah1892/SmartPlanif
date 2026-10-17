using SmartPlanif.Application.Abstractions;
using SmartPlanif.Domain.Allocation;
using SmartPlanif.Domain.Entities;
using SmartPlanif.Domain.Scheduling;

namespace SmartPlanif.Application.UseCases;

/// <summary>Statut d'un jour, d'après la seule couverture SQL.</summary>
public enum DayStatus { Empty, Unplanned, Partial, Complete }

/// <summary>Règles de calcul pures (testables sans base).</summary>
public static class KpiMath
{
    public static DayStatus StatusOf(DayCoverage c) =>
        c.Trips == 0 ? DayStatus.Empty
        : c.Complete == 0 && c.Partial == 0 ? DayStatus.Unplanned
        : c.Complete < c.Trips ? DayStatus.Partial
        : DayStatus.Complete;
}

public sealed record RuleCount(string Code, int Count);
public sealed record DayTripRef(long Id, string Line, DateTime Departure, DateTime Arrival);

/// <summary>KPI détaillés d'un jour : couverture, conformité (validation indépendante) et qualité.</summary>
public sealed record DayKpi(
    DateOnly Date, bool IsEditable, int Trips, int Planned, int Realized,
    IReadOnlyList<DayTripRef> Unplanned, int UnknownResources,
    IReadOnlyList<PlanViolation> Violations, IReadOnlyList<RuleCount> ViolationsByRule,
    int BusesUsed, int ActiveBuses, int AgentsUsed, int PlannableAgents,
    double AvgAgentHours, double StdAgentHours, double MaxAgentHours, int AgentsNearLimit,
    double? BusSameCenterRate, double? AgentSameCenterRate);

/// <summary>
/// Cas d'usage « tableau de bord » : relit ce qui est ENREGISTRÉ dans trips et le juge de façon indépendante du moteur.
/// Le plan effectif d'un jour = ressources réelles si le voyage est effectué, sinon ressources prévues.
/// </summary>
public sealed class KpiService(
    IKpiRepository kpi, ITripRepository trips, IResourceRepository resources,
    IAllocationSettingsStore settings, PlanningCalendar calendar)
{
    public Task<IReadOnlyList<DayCoverage>> GetCoverageAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        kpi.GetCoverageAsync(from, to, ct);

    public Task<PlanVsRealStats> GetPlanVsRealAsync(DateOnly from, DateOnly to, int onTimeSeconds, CancellationToken ct = default) =>
        kpi.GetPlanVsRealAsync(from, to, onTimeSeconds, ct);

    public Task<IReadOnlyList<PlanVsRealStats>> GetPlanVsRealByLineAsync(DateOnly from, DateOnly to, int onTimeSeconds, CancellationToken ct = default) =>
        kpi.GetPlanVsRealByLineAsync(from, to, onTimeSeconds, ct);

    /// <summary>KPI d'un jour. Les jours voisins (J-1, J+1) sont chargés pour contrôler repos et chevauchements.</summary>
    public async Task<DayKpi> GetDayAsync(DateOnly date, CancellationToken ct = default)
    {
        var policy = await settings.LoadAsync(ct);
        var dayTrips = await trips.GetTripsOfDayAsync(date, ct);
        var window = await trips.GetAssignedAsync(date.AddDays(-1), date.AddDays(1), ct);
        var pool = await resources.GetPoolAsync(date.AddDays(-1), date.AddDays(1), ct);

        var dayIds = dayTrips.Select(t => t.Id).ToHashSet();
        var buses = pool.Buses.ToDictionary(b => b.Id);
        var agents = pool.Agents.ToDictionary(a => a.Id);

        // Affectations dont les trois ressources existent dans le parc ; les autres sont comptées à part (clé étrangère cassée).
        bool Known(AssignedTrip a) => buses.ContainsKey(a.BusId) && agents.ContainsKey(a.DriverId) && agents.ContainsKey(a.ReceiverId);
        var known = window.Where(Known).ToList();
        var unknown = window.Count(a => !Known(a) && dayIds.Contains(a.Trip.Id));
        var dayKnown = known.Where(a => dayIds.Contains(a.Trip.Id)).ToList();
        var assignedDayIds = window.Where(a => dayIds.Contains(a.Trip.Id)).Select(a => a.Trip.Id).ToHashSet();

        // Contrôle indépendant : on rejoue les règles dures sur tout le plan de la fenêtre, puis on garde les violations du jour.
        var state = new AllocationState(known.Select(a => a.Trip).ToList(), new ScheduleBook());
        foreach (var a in known)
            state.Assign(new TripAssignment(a.Trip, buses[a.BusId], agents[a.DriverId], agents[a.ReceiverId], 0, 0, ""));
        var violations = PlanValidator.Validate(state, pool, new TripAllocator(policy))
            .Where(v => dayIds.Contains(v.TripId)).ToList();

        // Charge des agents sur le jour (chauffeur et receveur comptent chacun les heures du voyage).
        var hours = new Dictionary<long, double>();
        foreach (var a in dayKnown)
        {
            var h = a.Trip.Interval.Duration.TotalHours;
            hours[a.DriverId] = hours.GetValueOrDefault(a.DriverId) + h;
            hours[a.ReceiverId] = hours.GetValueOrDefault(a.ReceiverId) + h;
        }
        var plannable = pool.Agents.Where(a => a.Role.IsPlannable()).Select(a => hours.GetValueOrDefault(a.Id)).ToList();
        var avg = plannable.Count > 0 ? plannable.Average() : 0;
        var std = plannable.Count > 0 ? Math.Sqrt(plannable.Average(h => (h - avg) * (h - avg))) : 0;
        var limit = policy.MaxDailyWork.TotalHours;

        // Critères souples : même centre que la ligne (quand les deux valeurs sont connues).
        var busPairs = dayKnown.Select(a => (Res: buses[a.BusId].CenterId, Trip: a.Trip.CenterId))
            .Where(p => p.Res is not null && p.Trip is not null).ToList();
        var agentPairs = dayKnown
            .SelectMany(a => new[] { agents[a.DriverId].CenterId, agents[a.ReceiverId].CenterId }.Select(c => (Res: c, Trip: a.Trip.CenterId)))
            .Where(p => p.Res is not null && p.Trip is not null).ToList();

        return new DayKpi(
            date, calendar.IsEditable(date), dayTrips.Count, assignedDayIds.Count, window.Count(a => a.IsRealized && dayIds.Contains(a.Trip.Id)),
            dayTrips.Where(t => !assignedDayIds.Contains(t.Id)).OrderBy(t => t.Departure)
                .Select(t => new DayTripRef(t.Id, t.LineLabel, t.Departure, t.Arrival)).ToList(),
            unknown, violations,
            violations.GroupBy(v => v.Code).Select(g => new RuleCount(g.Key, g.Count())).OrderByDescending(r => r.Count).ToList(),
            dayKnown.Select(a => a.BusId).Distinct().Count(), pool.Buses.Count(b => b.Active),
            hours.Count, plannable.Count, avg, std, plannable.Count > 0 ? plannable.Max() : 0,
            plannable.Count(h => limit > 0 && h >= 0.9 * limit),
            busPairs.Count > 0 ? busPairs.Count(p => p.Res == p.Trip) / (double)busPairs.Count : null,
            agentPairs.Count > 0 ? agentPairs.Count(p => p.Res == p.Trip) / (double)agentPairs.Count : null);
    }
}