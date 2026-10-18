using SmartPlanif.Domain.Common;
using SmartPlanif.Domain.Constraints;
using SmartPlanif.Domain.Entities;

namespace SmartPlanif.Domain.Allocation;

/// <summary>Répartition d'une charge (voyages, km, heures) entre ressources comparables.</summary>
public sealed record DistributionStats(
    string Name, string Unit, int Count, int Unused,
    double Mean, double Min, double Max, double StdDev, double Cv, double Jain, double Gini)
{
    /// <summary>Verdict sur le coefficient de variation (à valider avec la société).</summary>
    public string Verdict => Cv <= 0.25 ? "Équitable" : Cv <= 0.5 ? "Acceptable" : "Déséquilibré";
    public string Css => Cv <= 0.25 ? "ok" : Cv <= 0.5 ? "warn" : "error";
}

public static class Distribution
{
    public static DistributionStats Describe(string name, string unit, IReadOnlyList<double> v)
    {
        var n = v.Count;
        if (n == 0) return new(name, unit, 0, 0, 0, 0, 0, 0, 0, 1, 0);
        var mean = v.Average();
        var std = Math.Sqrt(v.Average(x => (x - mean) * (x - mean)));
        var sum = v.Sum();
        var squares = v.Sum(x => x * x);
        var jain = squares > 0 ? sum * sum / (n * squares) : 1;          // 1 = parfaitement égal, 1/n = tout sur un seul

        var sorted = v.OrderBy(x => x).ToList();
        double gini = 0;
        if (sum > 0)
        {
            double acc = 0;
            for (var i = 0; i < n; i++) acc += (i + 1) * sorted[i];
            gini = 2 * acc / (n * sum) - (n + 1.0) / n;                   // 0 = égalité parfaite
        }
        return new(name, unit, n, v.Count(x => x <= 0), mean, sorted[0], sorted[^1], std, mean > 0 ? std / mean : 0, jain, gini);
    }
}

public sealed record FairnessReport(IReadOnlyList<DistributionStats> Indicators)
{
    /// <summary>Mesure l'équité du jour sur les ressources réellement disponibles (bus actifs, agents ni en repos ni en congé).</summary>
    public static FairnessReport Measure(AllocationState state, ResourcePool pool, DateOnly day, AllocationPolicy policy)
    {
        var start = day.ToDateTime(TimeOnly.MinValue);
        var end = start.AddDays(1);
        bool Free(ResourceKind k, long id) =>
            !pool.UnavailabilityFor(k, id).Any(u => u.Period.Start <= start && u.Period.End >= end);

        var restRule = ConstraintCatalog.FindDescriptor("A03");
        var restActive = restRule is not null && ConstraintCatalog.ResolveSetting(policy, restRule).Mode == ConstraintMode.Hard;

        var buses = pool.Buses.Where(b => b.Active && Free(ResourceKind.Bus, b.Id))
            .Select(b => state.Book.Bus(b.Id)).ToList();
        var agents = pool.Agents.Where(a => a.Role.IsPlannable() && Free(ResourceKind.Agent, a.Id)
                                            && !(restActive && a.RestDay == day.DayOfWeek))
            .Select(a => state.Book.Agent(a.Id)).ToList();

        return new FairnessReport(new[]
        {
            Distribution.Describe("Bus : voyages", "voyages", buses.Select(s => (double)s.TripCountOn(day)).ToList()),
            Distribution.Describe("Bus : kilomètres", "km", buses.Select(s => s.KmOn(day)).ToList()),
            Distribution.Describe("Bus : heures de service", "h", buses.Select(s => s.WorkedOn(day).TotalHours).ToList()),
            Distribution.Describe("Agents : heures de travail", "h", agents.Select(s => s.WorkedOn(day).TotalHours).ToList()),
            Distribution.Describe("Agents : postes tenus", "postes", agents.Select(s => (double)s.TripCountOn(day)).ToList()),
        });
    }
}