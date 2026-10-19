using SmartPlanif.Domain.Allocation;
using SmartPlanif.Domain.Entities;

namespace SmartPlanif.Application.Abstractions;

/// <summary>Élément de référence (type de ligne, catégorie de bus...) : identifiant + libellé.</summary>
public sealed record ReferenceItem(long Id, string Label,string LabelArabic);

/// <summary>Accès aux voyages (tables trips, stop_times, drligne).</summary>
public interface ITripRepository
{
    /// <summary>
    /// TOUS les voyages valides d'un jour de service (planifiés ou non). Le choix « lesquels faut-il (re)planifier ? »
    /// est fait par <c>DayPlanningInputs</c> selon le mode demandé, pas par le dépôt.
    /// </summary>
    Task<IReadOnlyList<Trip>> GetTripsOfDayAsync(DateOnly date, CancellationToken ct);

    /// <summary>
    /// Voyages dont les trois ressources sont connues sur la fenêtre [from, to] : ressources RÉELLES (*_RE) si elles sont
    /// toutes renseignées (voyage effectué), sinon ressources PRÉVUES (*_PR) si elles sont toutes renseignées.
    /// </summary>
    Task<IReadOnlyList<AssignedTrip>> GetAssignedAsync(DateOnly from, DateOnly to, CancellationToken ct);
}

/// <summary>Accès aux ressources (drvehic, dragent) et à leurs indisponibilités.</summary>
public interface IResourceRepository
{
    /// <summary>Bus, agents et indisponibilités couvrant la fenêtre [from, to].</summary>
    Task<ResourcePool> GetPoolAsync(DateOnly from, DateOnly to, CancellationToken ct);
}

/// <summary>Plan d'une journée à écrire dans la table trips (colonnes BUS_PR, CHAUFF_PR, REC_PR).</summary>
/// <param name="Date">Jour de service.</param>
/// <param name="Assignments">Affectations à écrire.</param>
/// <param name="UnassignedTripIds">Voyages du jour restés sans ressource.</param>
/// <param name="ClearUnassigned">Vider les colonnes *_PR des voyages non affectés (mode « remplacer la journée »).</param>
public sealed record DayPlanToSave(DateOnly Date, IReadOnlyList<TripAssignment> Assignments, IReadOnlyList<long> UnassignedTripIds, bool ClearUnassigned);

/// <summary>Résultat de l'écriture d'un plan de journée.</summary>
public sealed record DayPlanSaveResult(bool Success, string Message, int TripsAssigned, int TripsCleared);

/// <summary>Une ligne du plan enregistré, avec les libellés pour l'affichage.</summary>
public sealed record SavedTripAssignment(
    long TripId, string LineLabel, DateTime Departure, DateTime? Arrival,
    long? BusId, string? BusPlate, long? DriverId, string? DriverName, long? ReceiverId, string? ReceiverName,
    string? RealBusPlate, string? RealDriverName, string? RealReceiverName, bool HasRealResources);

/// <summary>Lecture et écriture du plan PRÉVU dans la table trips.</summary>
public interface IAssignmentRepository
{
    /// <summary>
    /// Écrit le plan d'une journée dans trips (BUS_PR, CHAUFF_PR, REC_PR) dans UNE transaction : tout ou rien.
    /// Les voyages déjà effectués (ressources réelles toutes renseignées) ne sont jamais modifiés.
    /// </summary>
    Task<DayPlanSaveResult> SaveDayPlanAsync(DayPlanToSave plan, string user, CancellationToken ct);

    /// <summary>Vide le plan PRÉVU d'une journée (hors voyages effectués). Renvoie le nombre de voyages modifiés.</summary>
    Task<int> ClearDayPlanAsync(DateOnly date, CancellationToken ct);

    /// <summary>Plan prévu enregistré pour un jour.</summary>
    Task<IReadOnlyList<SavedTripAssignment>> GetSavedPlanAsync(DateOnly date, CancellationToken ct);
}

/// <summary>Données de référence servant aux écrans de configuration (tables drtyli, drcatve).</summary>
public interface IReferenceDataRepository
{
    /// <summary>Types de ligne (urbain, interurbain...).</summary>
    Task<IReadOnlyList<ReferenceItem>> GetLineTypesAsync(CancellationToken ct);

    /// <summary>Catégories de véhicules (standard, confort...).</summary>
    Task<IReadOnlyList<ReferenceItem>> GetBusCategoriesAsync(CancellationToken ct);
}

/// <summary>Persistance de la politique d'allocation (choix des contraintes, paramètres, ALNS).</summary>
public interface IAllocationSettingsStore
{
    /// <summary>Charge la politique ; renvoie toujours un objet complet et indépendant (copie).</summary>
    Task<AllocationPolicy> LoadAsync(CancellationToken ct);

    /// <summary>Enregistre la politique.</summary>
    Task SaveAsync(AllocationPolicy policy, CancellationToken ct);

    /// <summary>Revient à la politique par défaut.</summary>
    Task ResetAsync(CancellationToken ct);
}

/// <summary>Nombre d'éléments par libellé (répartition d'une colonne).</summary>
public sealed record DiagnosticCount(string Label, long Count);

/// <summary>Statistiques d'une colonne de ressource de trips : lignes non NULL, et lignes avec un identifiant valide (> 0).</summary>
public sealed record ResourceColumnStat(string Column, long NonNull, long Positive)
{
    /// <summary>Lignes non NULL mais à 0 ou négatives : traitées comme « aucune ressource ».</summary>
    public long Zero => NonNull - Positive;
}

/// <summary>Durée déduite de l'itinéraire d'une ligne (table dritin), avec les valeurs lues pour vérification.</summary>
public sealed record LineDurationInfo(
    long LineId, string Label, long TripsOfDay, int Stops, long? MinMinutes, long? MaxMinutes, long? SumMinutes, long? MaxKm,
    double? ResolvedMinutes, string Method, double? SpeedKmh);

/// <summary>Constats bruts relevés directement dans la base pour un jour donné.</summary>
public sealed record RawDiagnostics(
    string DayMapping, long TotalTripsInTable, DateTime? FirstTripDay, DateTime? LastTripDay,
    long TripsOfDay, long TripsOfDayOnActiveLine, long TripsOfDayWithoutStopTimes, long TripsOfDayWithoutStopTimesAndItinerary,
    long TripsOfDayWithPlannedResources, long TripsOfDayWithRealResources,
    long BusesTotal, long BusesFlaggedActive,
    IReadOnlyList<ResourceColumnStat> ResourceColumns, IReadOnlyList<LineDurationInfo> LineDurations,
    IReadOnlyList<DiagnosticCount> TripStates, IReadOnlyList<DiagnosticCount> AgentQualifications,
    IReadOnlyList<DiagnosticCount> AgentRestDays, IReadOnlyList<DiagnosticCount> BusActivity,
    IReadOnlyList<DiagnosticCount> BusCategories, IReadOnlyList<DiagnosticCount> LineTypesOfDay,
    IReadOnlyList<string> SampleTrips, IReadOnlyList<string> MissingOptionalTables);

/// <summary>Relevés de diagnostic de la base (lecture seule).</summary>
public interface IDatabaseDiagnostics
{
    /// <summary>Collecte les constats bruts pour un jour de service.</summary>
    Task<RawDiagnostics> CollectAsync(DateOnly date, CancellationToken ct);
}

/// <summary>Poste tenu par un agent sur un voyage.</summary>
public enum AgentPost { None, Driver, Receiver }

/// <summary>Un agent (table dragent) avec sa délégation.</summary>
public sealed record AgentListItem(
    long Id, string? NameFr, string? NameAr, string? Qualification,
    int? RestDay, long? CenterId, long? DelegationId, string? DelegationLabel, long? Pda);

/// <summary>Un voyage d'un agent sur un jour : poste prévu (*_PR) et poste réel (*_RE).</summary>
public sealed record AgentTripRow(
    long TripId, string LineLabel, DateTime Departure, DateTime? Arrival, AgentPost Planned, AgentPost Real);

/// <summary>Lecture des agents (lecture seule).</summary>
public interface IAgentRepository
{
    /// <summary>Tous les agents.</summary>
    Task<IReadOnlyList<AgentListItem>> GetAgentsAsync(CancellationToken ct);

    /// <summary>Voyages d'un agent pour un jour de service (prévus ou réels).</summary>
    Task<IReadOnlyList<AgentTripRow>> GetAgentTripsAsync(long agentId, DateOnly date, CancellationToken ct);
}

#region Bus

/// <summary>Un bus (table drvehic) avec sa catégorie (drcatve) et sa délégation (drdeleg).</summary>
public sealed record BusListItem(
    long Id, string? Plate, long? CategoryId, string? CategoryLabel, string? CategoryLabelAr,
    int Seats, int Standing, DateTime? InServiceSince,
    long? CenterId, long? DelegationId, string? DelegationLabel, long? ActiveFlag)
{
    /// <summary>Capacité totale = places assises + debout.</summary>
    public int Capacity => Seats + Standing;
}

/// <summary>Lignes desservies par un bus sur un jour (ressources réelles, à défaut prévues).</summary>
public sealed record BusLineUsage(long BusId, long LineId, string LineLabel, int Trips);

/// <summary>Un voyage d'un bus sur un jour : prévu (BUS_PR) et/ou réel (BUS_RE).</summary>
public sealed record BusTripRow(long TripId, string LineLabel, DateTime Departure, DateTime? Arrival, bool Planned, bool Real);

/// <summary>Lecture des bus (lecture seule).</summary>
public interface IBusRepository
{
    /// <summary>Tous les bus.</summary>
    Task<IReadOnlyList<BusListItem>> GetBusesAsync(CancellationToken ct);

    /// <summary>Lignes desservies par chaque bus pour un jour de service.</summary>
    Task<IReadOnlyList<BusLineUsage>> GetBusLineUsageAsync(DateOnly date, CancellationToken ct);

    /// <summary>Voyages d'un bus pour un jour de service.</summary>
    Task<IReadOnlyList<BusTripRow>> GetBusTripsAsync(long busId, DateOnly date, CancellationToken ct);
}
#endregion

#region KPI's
/// <summary>Couverture d'un jour de service, calculée en SQL sur trips.</summary>
/// <param name="Complete">Voyages avec BUS_PR, CHAUFF_PR et REC_PR tous renseignés.</param>
/// <param name="Partial">Voyages avec au moins une ressource prévue mais pas les trois.</param>
public sealed record DayCoverage(DateOnly Day, int Trips, int Complete, int Partial, int Realized);

/// <summary>Changements d'un poste entre prévu et réel.</summary>
/// <param name="Planned">Voyages avec une ressource prévue (*_PR &gt; 0).</param>
/// <param name="Changed">Prévu ET réel renseignés, mais différents.</param>
/// <param name="NoReal">Prévu renseigné, réel absent (NULL ou 0) : sémantique à valider avec la société.</param>
public sealed record RoleChange(long Planned, long Changed, long NoReal)
{
    /// <summary>Taux strict : seuls les voyages où le réel est connu comptent.</summary>
    public double? Strict => Planned - NoReal > 0 ? (double)Changed / (Planned - NoReal) : null;

    /// <summary>Taux « littéral » du document (chapitre 7) : un réel absent compte comme un changement.</summary>
    public double? Literal => Planned > 0 ? (double)(Changed + NoReal) / Planned : null;
}

/// <summary>Indicateurs prévu vs réel (chapitre 7 du document de stratégie) pour une période ou une ligne.</summary>
public sealed record PlanVsRealStats(
    string Label, long Trips, long WithPlan, long WithReal, long PlanAndReal,
    RoleChange Bus, RoleChange Driver, RoleChange Receiver,
    long AnyChanged, long ChangeFlag, long DelayKnown, double? AvgDelaySeconds, long OnTime)
{
    /// <summary>Part des voyages planifiés (au moins une ressource prévue).</summary>
    public double? PlannedRate => Trips > 0 ? (double)WithPlan / Trips : null;

    /// <summary>Taux global de changement : au moins un poste modifié, parmi les voyages ayant prévu et réel.</summary>
    public double? GlobalChangeRate => PlanAndReal > 0 ? (double)AnyChanged / PlanAndReal : null;

    /// <summary>Part des voyages à l'heure parmi ceux dont le retard est connu.</summary>
    public double? OnTimeRate => DelayKnown > 0 ? (double)OnTime / DelayKnown : null;
}

/// <summary>Requêtes d'agrégation pour les KPI (lecture seule).</summary>
public interface IKpiRepository
{
    /// <summary>Couverture jour par jour sur [from, to] (jours sans voyage absents).</summary>
    Task<IReadOnlyList<DayCoverage>> GetCoverageAsync(DateOnly from, DateOnly to, CancellationToken ct);

    /// <summary>Indicateurs prévu vs réel sur [from, to]. <paramref name="onTimeSeconds"/> : retard toléré pour « à l'heure ».</summary>
    Task<PlanVsRealStats> GetPlanVsRealAsync(DateOnly from, DateOnly to, int onTimeSeconds, CancellationToken ct);

    /// <summary>Mêmes indicateurs par ligne (les plus modifiées d'abord).</summary>
    Task<IReadOnlyList<PlanVsRealStats>> GetPlanVsRealByLineAsync(DateOnly from, DateOnly to, int onTimeSeconds, CancellationToken ct);

    Task<IReadOnlyList<DayQuality>> GetDailyQualityAsync(DateOnly from, DateOnly to, int onTimeSeconds, CancellationToken ct);
    Task<IReadOnlyList<ResourceUsage>> GetBusUsageAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<IReadOnlyList<ResourceUsage>> GetAgentUsageAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<IReadOnlyList<ResourceDay>> GetResourceDailyAsync(bool isBus, long id, DateOnly from, DateOnly to, CancellationToken ct);
}

/// <summary>Qualité d'un jour de service (plan effectif = réel si renseigné, sinon prévu).</summary>
public sealed record DayQuality(
    DateOnly Day, int Trips, int Planned, int Realized, int BusesUsed, int AgentsUsed,
    long DelayKnown, long OnTime, double? AvgDelaySeconds, long PlanAndReal, long Changed);

/// <summary>Utilisation d'une ressource (bus ou agent) sur une période.</summary>
public sealed record ResourceUsage(long Id, string Label, int Trips, int DaysUsed);

/// <summary>Voyages d'une ressource sur un jour.</summary>
public sealed record ResourceDay(DateOnly Day, int Trips);
#endregion

#region Congé (Leave)
/// <summary>Congé / absence d'un agent (ligne KIND='AGENT' de resource_unavailability). EndAt est EXCLU (minuit du lendemain du dernier jour).</summary>
public sealed record AgentLeave(
    long Id, long AgentId, DateTime StartAt, DateTime EndAt, string Type, string? Reason, string? CreatedBy, DateTime? CreatedAt)
{
    public DateOnly FirstDay => DateOnly.FromDateTime(StartAt);
    public DateOnly LastDay => DateOnly.FromDateTime(EndAt.AddTicks(-1));
    public int Days => LastDay.DayNumber - FirstDay.DayNumber + 1;
}

/// <summary>Saisie d'un congé, en jours entiers (premier et dernier jour inclus).</summary>
public sealed record AgentLeaveDraft(long AgentId, string Type, DateOnly FirstDay, DateOnly LastDay, string? Comment);

/// <summary>Voyage déjà planifié (prévu) qui tombe pendant un congé.</summary>
public sealed record LeaveConflict(long TripId, string LineLabel, DateTime Departure, AgentPost Post);

/// <summary>Lecture et écriture des congés des agents.</summary>
public interface ILeaveRepository
{
    /// <summary>Vrai si la table resource_unavailability existe avec la colonne LEAVE_TYPE (script sql/004).</summary>
    Task<bool> IsReadyAsync(CancellationToken ct);

    /// <summary>Congés qui chevauchent [from, to[ ; pour un agent, ou pour tous si <paramref name="agentId"/> est null.</summary>
    Task<IReadOnlyList<AgentLeave>> GetLeavesAsync(long? agentId, DateTime from, DateTime to, CancellationToken ct);

    /// <summary>Ajoute un congé et renvoie son identifiant.</summary>
    Task<long> AddAsync(AgentLeave leave, CancellationToken ct);

    /// <summary>Supprime un congé, seulement s'il n'est pas terminé avant <paramref name="notEndedBefore"/>.</summary>
    Task<bool> DeleteAsync(long id, DateTime notEndedBefore, CancellationToken ct);

    /// <summary>Voyages prévus (hors effectués) de l'agent entre deux jours inclus.</summary>
    Task<IReadOnlyList<LeaveConflict>> GetConflictsAsync(long agentId, DateOnly firstDay, DateOnly lastDay, CancellationToken ct);
}


#endregion

#region Lignes

/// <summary>Une ligne (table drligne) avec son type (drtyli) et sa délégation (drdeleg).</summary>
public sealed record LineListItem(
    long Id, string? NameFr, string? NameAr,
    long? TypeId, string? TypeLabel, string? TypeLabelAr,
    double? Km, long? CenterId, long? DelegationId, string? DelegationLabel,
    long? Nature, string? TariffType, bool Active, bool Sae, int Stops);

/// <summary>Couverture d'une ligne sur un jour : voyages, voyages avec plan prévu complet, voyages effectués.</summary>
public sealed record LineDayStats(long LineId, int Trips, int Planned, int Realized);

/// <summary>Un arrêt de l'itinéraire (dritin + drstati).</summary>
public sealed record LineStopRow(
    long Sequence, long? StationId, string? NameFr, string? NameAr,
    long? Km, long? Minutes, double? Lat, double? Lon, long? Section);

/// <summary>Itinéraire d'une ligne, avec la durée retenue par le moteur et la vitesse moyenne implicite.</summary>
public sealed record LineItinerary(IReadOnlyList<LineStopRow> Stops, double? DurationMinutes, string Method, double? SpeedKmh);

/// <summary>Un voyage d'une ligne sur un jour, avec le plan prévu et les ressources réelles.</summary>
public sealed record LineTripRow(
    long TripId, DateTime Departure, DateTime? Arrival, int Direction,
    string? BusPlate, string? DriverName, string? ReceiverName,
    string? RealBusPlate, string? RealDriverName, string? RealReceiverName, bool HasRealResources);

/// <summary>Lecture des lignes (lecture seule).</summary>
public interface ILineRepository
{
    /// <summary>Toutes les lignes.</summary>
    Task<IReadOnlyList<LineListItem>> GetLinesAsync(CancellationToken ct);

    /// <summary>Voyages, plans complets et voyages effectués par ligne pour un jour de service.</summary>
    Task<IReadOnlyList<LineDayStats>> GetDayStatsAsync(DateOnly date, CancellationToken ct);

    /// <summary>Itinéraire d'une ligne (arrêts, km, minutes, GPS) et durée retenue.</summary>
    Task<LineItinerary> GetItineraryAsync(long lineId, CancellationToken ct);

    /// <summary>Voyages d'une ligne pour un jour de service.</summary>
    Task<IReadOnlyList<LineTripRow>> GetLineTripsAsync(long lineId, DateOnly date, CancellationToken ct);
}

#endregion