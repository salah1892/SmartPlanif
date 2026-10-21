//using SmartPlanif.Application.Abstractions;
//using SmartPlanif.Domain.Allocation;
//using SmartPlanif.Domain.Entities;

//namespace SmartPlanif.Application.UseCases;

///// <summary>Résultat de la déclaration d'un aléa : voyages impactés identifiés.</summary>
//public sealed record IncidentImpact(
//    IncidentDeclaration Incident, IReadOnlyList<ImpactedTrip> ImpactedTrips,
//    int TotalTripsOfDay, int RealizedTrips, string Message);

///// <summary>Résultat complet d'une réaffectation : aléa, nettoyage, réallocation.</summary>
//public sealed record ReassignmentReport(
//    IncidentDeclaration Incident,
//    IReadOnlyList<ImpactedTrip> ImpactedTrips,
//    int TripsCleared,
//    AllocationReport? Reallocation,
//    string Message);

///// <summary>
///// Cas d'usage « réaffectation dynamique » : un aléa survient en cours de journée (panne, absence...).
///// Identifie les voyages impactés, les libère, puis réalloue UNIQUEMENT ces voyages en respectant
///// toutes les autres affectations existantes (qui restent verrouillées).
///// </summary>
//public sealed class ReassignHandler(
//    ITripRepository trips, IResourceRepository resources, IAssignmentRepository assignments,
//    IAllocationSettingsStore settings, PlanningCalendar calendar, ILeaveRepository leaves)
//{
//    /// <summary>
//    /// Étape 1 : identifier les voyages impactés par l'aléa (sans rien modifier).
//    /// Un voyage est impacté s'il est planifié avec la ressource défaillante, pas encore effectué,
//    /// et dont le départ est postérieur à l'heure de l'aléa.
//    /// </summary>
//    public async Task<IncidentImpact> IdentifyImpactAsync(IncidentDeclaration incident, CancellationToken ct)
//    {
//        calendar.EnsureEditable(incident.Date);

//        var saved = await assignments.GetSavedPlanAsync(incident.Date, ct);
//        var now = incident.FromTime;

//        // Voyages déjà effectués (jamais touchés) et voyages dont le départ est passé.
//        var impacted = new List<ImpactedTrip>();
//        foreach (var row in saved)
//        {
//            if (row.HasRealResources) continue;
//            if (row.Departure <= now) continue;
//            if (row.BusId is null && row.DriverId is null && row.ReceiverId is null) continue;

//            var role = incident.Kind switch
//            {
//                IncidentKind.BusBreakdown when row.BusId == incident.ResourceId => "BUS",
//                IncidentKind.DriverAbsence when row.DriverId == incident.ResourceId => "CHAUFFEUR",
//                IncidentKind.ReceiverAbsence when row.ReceiverId == incident.ResourceId => "RECEVEUR",
//                _ => null
//            };

//            if (role is not null)
//                impacted.Add(new ImpactedTrip(row.TripId, row.LineLabel, row.Departure, row.Arrival, role));
//        }

//        var totalOfDay = saved.Count;
//        var realized = saved.Count(r => r.HasRealResources);

//        var message = impacted.Count == 0
//            ? "Aucun voyage futur n'est affecté par cette ressource après l'heure indiquée."
//            : $"{impacted.Count} voyage(s) à réaffecter sur {totalOfDay} au total ({realized} déjà effectués, intouchables).";

//        return new IncidentImpact(incident, impacted, totalOfDay, realized, message);
//    }

//    /// <summary>
//    /// Étape 2 : exécuter la réaffectation complète.
//    /// 1. Enregistre l'indisponibilité dans resource_unavailability.
//    /// 2. Libère les voyages impactés (colonnes *_PR à NULL).
//    /// 3. Réalloue uniquement ces voyages en mode FillMissingOnly (le reste est verrouillé).
//    /// </summary>
//    public async Task<ReassignmentReport> ExecuteAsync(
//        IncidentDeclaration incident, IReadOnlyList<long> tripIdsToClear,
//        AllocationMethod method, string user, CancellationToken ct)
//    {
//        calendar.EnsureEditable(incident.Date);

//        // 1. Déclarer l'indisponibilité (pour que le moteur exclue la ressource).
//        var endOfDay = incident.Date.ToDateTime(TimeOnly.MaxValue);
//        var kind = incident.Kind == IncidentKind.BusBreakdown ? "BUS" : "AGENT";
//        var leaveType = incident.Kind == IncidentKind.BusBreakdown ? "MAINTENANCE" : "ABSENCE";
//        var unavailability = new AgentLeave(
//            0, incident.ResourceId, incident.FromTime, endOfDay,
//            leaveType, incident.Reason, user, null);

//        // On utilise le repository des congés qui écrit dans resource_unavailability.
//        // Pour un bus, on écrit directement (le repo leave est pour les agents).
//        // Ici on simplifie : on passe par le leave repo qui gère les AGENT.
//        // Pour BUS, il faudrait un accès direct — voir note ci-dessous.
//        if (kind == "AGENT")
//        {
//            await leaves.AddAsync(unavailability, ct);
//        }
//        // NOTE : Pour BUS, il faudrait ajouter une méthode dans IResourceRepository
//        // ou utiliser directement le SQL. Voir Étape 4b ci-dessous.

//        // 2. Libérer les voyages impactés.
//        var cleared = await assignments.ClearTripsAsync(tripIdsToClear, ct);

//        // 3. Réallouer en mode FillMissingOnly : seuls les voyages sans plan seront traités.
//        var policy = await settings.LoadAsync(ct);
//        var command = new AllocateDayCommand(
//            incident.Date, user, method,
//            SaveMode.AllowPartial,
//            ExistingPlanMode.FillMissingOnly,
//            PolicyOverride: policy);

//        // On réutilise le handler d'allocation existant.
//        var allocator = new AllocateDayHandler(trips, resources, assignments,
//            new NullReferenceData(), settings, calendar);
//        var reallocation = await allocator.HandleAsync(command, null, ct);

//        var message = $"Réaffectation terminée : {cleared} voyage(s) libéré(s), " +
//                      $"{reallocation.Assignments.Count} réaffecté(s), " +
//                      $"{reallocation.UnassignedCount} encore sans ressource.";

//        return new ReassignmentReport(incident, Array.Empty<ImpactedTrip>(), cleared, reallocation, message);
//    }
//}

///// <summary>Placeholder pour IReferenceDataRepository dans le contexte de réaffectation.</summary>
//internal sealed class NullReferenceData : IReferenceDataRepository
//{
//    public Task<IReadOnlyList<ReferenceItem>> GetLineTypesAsync(CancellationToken ct) =>
//        Task.FromResult<IReadOnlyList<ReferenceItem>>(Array.Empty<ReferenceItem>());
//    public Task<IReadOnlyList<ReferenceItem>> GetBusCategoriesAsync(CancellationToken ct) =>
//        Task.FromResult<IReadOnlyList<ReferenceItem>>(Array.Empty<ReferenceItem>());
//}

using SmartPlanif.Application.Abstractions;
using SmartPlanif.Domain.Allocation;
using SmartPlanif.Domain.Entities;

namespace SmartPlanif.Application.UseCases;

/// <summary>Résultat de la déclaration d'un aléa : voyages impactés identifiés.</summary>
public sealed record IncidentImpact(
    IncidentDeclaration Incident, IReadOnlyList<ImpactedTrip> ImpactedTrips,
    int TotalTripsOfDay, int RealizedTrips, string Message);

/// <summary>Résultat complet d'une réaffectation : aléa, nettoyage, réallocation.</summary>
public sealed record ReassignmentReport(
    IncidentDeclaration Incident,
    IReadOnlyList<ImpactedTrip> ImpactedTrips,
    int TripsCleared,
    AllocationReport? Reallocation,
    string Message);

/// <summary>
/// Cas d'usage « réaffectation dynamique » : un aléa survient en cours de journée (panne, absence...).
/// Identifie les voyages impactés, les libère, puis réalloue UNIQUEMENT ces voyages en respectant
/// toutes les autres affectations existantes (qui restent verrouillées).
/// </summary>
public sealed class ReassignHandler(
    ITripRepository trips, IResourceRepository resources, IAssignmentRepository assignments,
    IReferenceDataRepository reference, IAllocationSettingsStore settings,
    PlanningCalendar calendar, ILeaveRepository leaves, AllocateDayHandler allocator)
{
    /// <summary>
    /// Étape 1 : identifier les voyages impactés par l'aléa (sans rien modifier).
    /// Un voyage est impacté s'il est planifié avec la ressource défaillante, pas encore effectué,
    /// et dont le départ est postérieur à l'heure de l'aléa.
    /// </summary>
    public async Task<IncidentImpact> IdentifyImpactAsync(IncidentDeclaration incident, CancellationToken ct)
    {
        calendar.EnsureEditable(incident.Date);

        var saved = await assignments.GetSavedPlanAsync(incident.Date, ct);
        var now = incident.FromTime;

        var impacted = new List<ImpactedTrip>();
        foreach (var row in saved)
        {
            if (row.HasRealResources) continue;
            if (row.Departure <= now) continue;
            if (row.BusId is null && row.DriverId is null && row.ReceiverId is null) continue;

            var role = incident.Kind switch
            {
                IncidentKind.BusBreakdown when row.BusId == incident.ResourceId => "BUS",
                IncidentKind.DriverAbsence when row.DriverId == incident.ResourceId => "CHAUFFEUR",
                IncidentKind.ReceiverAbsence when row.ReceiverId == incident.ResourceId => "RECEVEUR",
                _ => null
            };

            if (role is not null)
                impacted.Add(new ImpactedTrip(row.TripId, row.LineLabel, row.Departure, row.Arrival, role));
        }

        var totalOfDay = saved.Count;
        var realized = saved.Count(r => r.HasRealResources);

        var message = impacted.Count == 0
            ? "Aucun voyage futur n'est affecté par cette ressource après l'heure indiquée."
            : $"{impacted.Count} voyage(s) à réaffecter sur {totalOfDay} au total ({realized} déjà effectués, intouchables).";

        return new IncidentImpact(incident, impacted, totalOfDay, realized, message);
    }

    /// <summary>
    /// Étape 2 : exécuter la réaffectation complète.
    /// 1. Enregistre l'indisponibilité dans resource_unavailability.
    /// 2. Libère les voyages impactés (colonnes *_PR à NULL).
    /// 3. Réalloue uniquement ces voyages en mode FillMissingOnly (le reste est verrouillé).
    /// </summary>
    public async Task<ReassignmentReport> ExecuteAsync(
        IncidentDeclaration incident, IReadOnlyList<long> tripIdsToClear,
        AllocationMethod method, string user, CancellationToken ct)
    {
        calendar.EnsureEditable(incident.Date);

        // 1. Déclarer l'indisponibilité.
        var endOfDay = incident.Date.AddDays(1).ToDateTime(TimeOnly.MinValue);
        if (incident.Kind == IncidentKind.BusBreakdown)
        {
            await leaves.AddBusUnavailabilityAsync(
                incident.ResourceId, incident.FromTime, endOfDay, incident.Reason, user, ct);
        }
        else
        {
            var leaveType = incident.Kind == IncidentKind.DriverAbsence ? "ABSENCE" : "ABSENCE";
            var leave = new AgentLeave(0, incident.ResourceId, incident.FromTime, endOfDay,
                leaveType, incident.Reason, user, null);
            await leaves.AddAsync(leave, ct);
        }

        // 2. Libérer les voyages impactés.
        var cleared = await assignments.ClearTripsAsync(tripIdsToClear, ct);

        // 3. Réallouer en mode FillMissingOnly : seuls les voyages sans plan seront traités.
        var command = new AllocateDayCommand(
            incident.Date, user, method,
            SaveMode.AllowPartial,
            ExistingPlanMode.FillMissingOnly);

        var reallocation = await allocator.HandleAsync(command, null, ct);

        var message = $"Réaffectation terminée : {cleared} voyage(s) libéré(s), " +
                      $"{reallocation.Assignments.Count} réaffecté(s), " +
                      $"{reallocation.UnassignedCount} encore sans ressource.";

        return new ReassignmentReport(incident, Array.Empty<ImpactedTrip>(), cleared, reallocation, message);
    }
}