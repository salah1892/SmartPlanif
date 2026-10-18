using System.Text;
using SmartPlanif.Domain.Constraints;
using SmartPlanif.Domain.Entities;
using SmartPlanif.Domain.Rules;
using SmartPlanif.Domain.Scheduling;

namespace SmartPlanif.Domain.Allocation;

public sealed record CriterionLine(string Code, string Name, double Weight, double Value, double Points, string? Note);
public sealed record CriterionDelta(string Code, string Name, double Delta, string? ChosenNote, string? OtherNote);
public sealed record AlternativeView(long Id, string Label, double Score, double Gap, IReadOnlyList<CriterionDelta> Deltas);
public sealed record RejectionGroup(string Code, string Name, int Count, IReadOnlyList<string> Examples);

public sealed record ResourceDecision(
    string Role, long ChosenId, string ChosenLabel, double Score, double MaxScore,
    int Rank, int EligibleCount, int TotalCount, int RejectedCount,
    double ChosenHours, double AverageHours,
    IReadOnlyList<CriterionLine> Criteria, IReadOnlyList<AlternativeView> Alternatives,
    IReadOnlyList<RejectionGroup> Rejections, string Summary);

public sealed record TripExplanation(ResourceDecision Bus, ResourceDecision Driver, ResourceDecision Receiver);

/// <summary>Explique, a posteriori, le choix de chaque ressource dans le plan FINAL (tient compte des autres voyages du plan).</summary>
public static class DecisionExplainer
{
    private sealed record Scored(long Id, string Label, IReadOnlyList<ScoreDetail> Details, double Score, double Hours);

    /// <summary><paramref name="book"/> : plannings du plan final (copie de travail, modifiée puis restaurée).</summary>
    public static TripExplanation Explain(TripAssignment a, ResourcePool pool, ScheduleBook book, TripAllocator allocator)
    {
        book.Remove(a.Trip, a.Bus.Id, a.Driver.Id, a.Receiver.Id);   // on se place « juste avant » ce voyage
        try
        {
            var bus = ExplainRole("Bus", a.Bus.Id, TripAllocator.BuildBusCandidates(pool, book), b => b.Plate,
                a.Trip, allocator.BusRules, allocator.Policy, new HashSet<long>());
            var driver = ExplainRole("Chauffeur", a.Driver.Id, TripAllocator.BuildAgentCandidates(pool, book, true), x => x.Name,
                a.Trip, allocator.AgentRules, allocator.Policy, new HashSet<long> { a.Receiver.Id });
            var receiver = ExplainRole("Receveur", a.Receiver.Id, TripAllocator.BuildAgentCandidates(pool, book, false), x => x.Name,
                a.Trip, allocator.AgentRules, allocator.Policy, new HashSet<long> { a.Driver.Id });
            return new TripExplanation(bus, driver, receiver);
        }
        finally
        {
            book.Commit(a.Trip, a.Bus.Id, a.Driver.Id, a.Receiver.Id);
        }
    }

    private static ResourceDecision ExplainRole<T>(
        string role, long chosenId, IEnumerable<Candidate<T>> candidates, Func<T, string> label,
        Trip trip, RuleSet<T> rules, AllocationPolicy policy, ISet<long> excluded)
    {
        var day = trip.WorkDate;
        var eligible = new List<Scored>();
        var rejected = new Dictionary<string, List<string>>();
        var hours = new List<double>();
        Scored? chosen = null;
        var total = 0;

        foreach (var c in candidates.Where(c => !excluded.Contains(c.Id)))
        {
            total++;
            var worked = c.Schedule.WorkedOn(day).TotalHours;
            hours.Add(worked);

            var ctx = new RuleContext<T>(c.Resource, trip, c.Schedule, c.Unavailabilities, policy);
            var violation = CandidateSelector.FindFirstViolation(ctx, rules);
            if (violation is not null && c.Id != chosenId)
            {
                if (!rejected.TryGetValue(violation.Code, out var list)) rejected[violation.Code] = list = new();
                list.Add(label(c.Resource));
                continue;
            }

            var details = rules.Soft
                .Select(r => { var v = Math.Clamp(r.Evaluate(ctx), 0, 1); return new ScoreDetail(r.Code, r.Weight, v, r.Explain(ctx, v)); })
                .ToList();
            var scored = new Scored(c.Id, label(c.Resource), details, details.Sum(d => d.Points), worked);
            if (c.Id == chosenId) chosen = scored;
            if (violation is null) eligible.Add(scored);
        }

        chosen ??= new Scored(chosenId, $"#{chosenId}", Array.Empty<ScoreDetail>(), 0, 0);
        var ordered = eligible.OrderByDescending(x => x.Score)
            .ThenBy(x => CandidateSelector.TieKey(policy, trip.Id, x.Id)).ToList();
        var rank = ordered.FindIndex(x => x.Id == chosenId) + 1;

        var alternatives = ordered.Where(x => x.Id != chosenId).Take(3).Select(alt =>
        {
            var deltas = chosen.Details.Select(cd =>
            {
                var ad = alt.Details.FirstOrDefault(d => d.Code == cd.Code);
                return new CriterionDelta(cd.Code, NameOf(cd.Code), cd.Points - (ad?.Points ?? 0), cd.Note, ad?.Note);
            }).Where(d => Math.Abs(d.Delta) >= 0.05).OrderByDescending(d => Math.Abs(d.Delta)).Take(3).ToList();
            return new AlternativeView(alt.Id, alt.Label, alt.Score, chosen.Score - alt.Score, deltas);
        }).ToList();

        var criteria = chosen.Details.OrderByDescending(d => d.Weight)
            .Select(d => new CriterionLine(d.Code, NameOf(d.Code), d.Weight, d.Value, d.Points, d.Note)).ToList();
        var rejections = rejected.Select(kv => new RejectionGroup(kv.Key, NameOf(kv.Key), kv.Value.Count, kv.Value.Take(5).ToList()))
            .OrderByDescending(r => r.Count).ToList();

        var max = rules.MaxScore;
        var summary = Summarize(role, chosen.Label, rank, eligible.Count, total, criteria, alternatives);
        return new ResourceDecision(role, chosenId, chosen.Label, chosen.Score, max, rank, eligible.Count, total,
            total - eligible.Count, chosen.Hours, hours.Count > 0 ? hours.Average() : 0,
            criteria, alternatives, rejections, summary);
    }

    private static string NameOf(string code) => ConstraintCatalog.FindDescriptor(code)?.Name ?? code;

    private static string Summarize(string role, string name, int rank, int eligible, int total,
        IReadOnlyList<CriterionLine> criteria, IReadOnlyList<AlternativeView> alts)
    {
        var sb = new StringBuilder();
        var eliminated = total - eligible;
        sb.Append($"{name} : {eligible} candidat(s) éligible(s) sur {total}");
        sb.Append(eliminated > 0 ? $" ({eliminated} éliminé(s) par une règle dure). " : ". ");

        var strengths = criteria.Where(c => c.Weight > 0 && c.Value >= 0.75).OrderByDescending(c => c.Points).Take(2).ToList();
        if (strengths.Count > 0)
            sb.Append("Points forts : " + string.Join(", ", strengths.Select(c => $"{c.Name.ToLower()} (+{c.Points:0.#})")) + ". ");

        if (eligible <= 1 || alts.Count == 0)
        {
            sb.Append("Seul candidat éligible : aucun autre choix possible.");
        }
        else if (rank == 1)
        {
            var alt = alts[0];
            if (alt.Gap < 0.05)
                sb.Append($"À égalité de score avec {alt.Label} : départagé par rotation (pour ne pas favoriser toujours le même).");
            else
            {
                var decisive = alt.Deltas.FirstOrDefault(d => d.Delta > 0);
                sb.Append($"Devance {alt.Label} de {alt.Gap:0.#} pts" +
                          (decisive is null ? "." : $", surtout grâce à « {decisive.Name.ToLower()} »."));
            }
        }
        else
        {
            sb.Append($"Le meilleur score local est {alts[0].Label} (+{-alts[0].Gap:0.#} pts) : ce choix vient de l'optimisation globale " +
                      "(équité de charge, voyages voisins) qui évite de surcharger ce candidat.");
        }
        return sb.ToString();
    }
}

/// <summary>Garde un instantané du plan final pour expliquer un voyage à la demande (aucun coût pendant l'allocation).</summary>
public sealed class PlanExplainer
{
    private readonly AllocationState _state;
    private readonly ResourcePool _pool;
    private readonly TripAllocator _allocator;
    private readonly object _gate = new();

    public PlanExplainer(AllocationState state, ResourcePool pool, TripAllocator allocator)
    {
        _state = state.Clone(); _pool = pool; _allocator = allocator;
    }

    public TripExplanation? Explain(long tripId)
    {
        lock (_gate)
            return _state.Assignments.TryGetValue(tripId, out var a)
                ? DecisionExplainer.Explain(a, _pool, _state.Book, _allocator) : null;
    }
}