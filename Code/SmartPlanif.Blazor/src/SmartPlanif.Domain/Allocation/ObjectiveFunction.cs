using SmartPlanif.Domain.Entities;
using SmartPlanif.Domain.Scheduling;

namespace SmartPlanif.Domain.Allocation;

/// <summary>
/// Fonction objectif hiérarchique (à MINIMISER) :
/// pénalité x voyages non affectés + déficit de score souple + poids x équité de charge.
/// La pénalité domine tout : un meilleur score n'est jamais obtenu en sacrifiant un voyage.
/// Les scores utilisés sont ceux calculés à l'insertion (approximation volontaire pour la vitesse) ;
/// les scores exacts sont recalculés une fois à la fin par <see cref="PlanEvaluator"/>.
/// </summary>
public sealed class ObjectiveFunction
{
    private readonly AllocationPolicy _policy;
    private readonly IReadOnlyList<long> _agentIds;
    private readonly IReadOnlyList<long> _busIds;
    /// <summary>Prépare la fonction pour un jeu de ressources donné.</summary>
    public ObjectiveFunction(AllocationPolicy policy, ResourcePool pool)
    {
        _policy = policy;
        // Un seul groupe : les chauffeurs et receveurs sont les mêmes personnes (ou se partagent la charge de la même équipe).
        _agentIds = pool.Agents.Where(a => a.Role.IsPlannable()).Select(a => a.Id).ToList();

        _busIds = pool.Buses.Where(b => b.Active).Select(b => b.Id).ToList();
    }

    /// <summary>Valeur de l'objectif pour un état (plus petit = meilleur).</summary>
    public double Evaluate(AllocationState state)
    {
        var scoreDeficit = 0.0;
        foreach (var assignment in state.Assignments.Values)
            scoreDeficit += assignment.MaxScore - assignment.Score;

        //var fairness = StandardDeviationOfHours(state.Book, _agentIds);

        //return _policy.Alns.UnassignedPenalty * state.UnassignedCount
        //     + scoreDeficit
        //     + _policy.Alns.FairnessWeight * fairness;
        var fairness = StdDev(state.Book, _agentIds, isBus: false);
        var busFairness = StdDev(state.Book, _busIds, isBus: true);
        return _policy.Alns.UnassignedPenalty * state.UnassignedCount
             + scoreDeficit
             + _policy.Alns.FairnessWeight * fairness
             + _policy.Alns.BusFairnessWeight * busFairness;
    }

    /// <summary>Écart-type des heures de travail d'un groupe d'agents : mesure d'inéquité.</summary>
    private static double StandardDeviationOfHours(ScheduleBook book, IReadOnlyList<long> agentIds)
    {
        if (agentIds.Count == 0) return 0;
        var hours = agentIds.Select(id => book.Agent(id).TotalWorked().TotalHours).ToList();
        var mean = hours.Average();
        return Math.Sqrt(hours.Average(h => (h - mean) * (h - mean)));
    }

    private static double StdDev(ScheduleBook book, IReadOnlyList<long> ids, bool isBus)
    {
        if (ids.Count == 0) return 0;
        var hours = ids.Select(id => (isBus ? book.Bus(id) : book.Agent(id)).TotalWorked().TotalHours).ToList();
        var mean = hours.Average();
        return Math.Sqrt(hours.Average(h => (h - mean) * (h - mean)));
    }
}
