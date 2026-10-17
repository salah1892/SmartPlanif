using SmartPlanif.Application.Abstractions;

namespace SmartPlanif.Application.UseCases;

/// <summary>Types de congé (codes stockés dans resource_unavailability.LEAVE_TYPE).</summary>
public static class LeaveTypes
{
    public static readonly IReadOnlyList<(string Code, string Label)> All = new (string, string)[]
    {
        ("CONGE", "Congé annuel"), ("MALADIE", "Congé maladie"), ("ABSENCE", "Absence"),
        ("SUSPENSION", "Suspension"), ("FORMATION", "Formation"), ("AUTRE", "Autre"),
    };

    public static string Label(string? code) => All.FirstOrDefault(t => t.Code == code).Label ?? "Autre";
    public static bool IsKnown(string? code) => All.Any(t => t.Code == code);
}

/// <summary>Résultat du contrôle d'une saisie : erreurs bloquantes et voyages déjà planifiés en conflit.</summary>
public sealed record LeaveCheck(IReadOnlyList<string> Errors, IReadOnlyList<LeaveConflict> Conflicts)
{
    public bool Ok => Errors.Count == 0;
}

public sealed record LeaveActionResult(bool Success, string Message);

/// <summary>Règles de validation (fonction pure, testable sans base).</summary>
public static class LeaveRules
{
    public const int MaxDays = 366;
    public const int MaxCommentLength = 100;

    public static IReadOnlyList<string> Validate(AgentLeaveDraft d, IEnumerable<AgentLeave> existing, DateOnly today)
    {
        var errors = new List<string>();
        if (!LeaveTypes.IsKnown(d.Type)) errors.Add("Type de congé inconnu.");
        if (d.LastDay < d.FirstDay) errors.Add("La date de fin doit être postérieure ou égale à la date de début.");
        else if (d.LastDay.DayNumber - d.FirstDay.DayNumber + 1 > MaxDays) errors.Add($"Un congé ne peut pas dépasser {MaxDays} jours.");
        if (d.LastDay < today)
            errors.Add($"Ce congé est entièrement passé (aujourd'hui : {today:dd/MM/yyyy}) : il n'est plus modifiable.");
        if ((d.Comment?.Trim().Length ?? 0) > MaxCommentLength) errors.Add($"Le commentaire est limité à {MaxCommentLength} caractères.");

        var overlap = existing.FirstOrDefault(e => d.FirstDay <= e.LastDay && d.LastDay >= e.FirstDay);
        if (overlap is not null)
            errors.Add($"Chevauche un congé existant ({LeaveTypes.Label(overlap.Type)} du {overlap.FirstDay:dd/MM/yyyy} au {overlap.LastDay:dd/MM/yyyy}).");
        return errors;
    }
}

/// <summary>Cas d'usage « consulter et affecter les congés des agents ».</summary>
public sealed class LeaveService(ILeaveRepository leaves, PlanningCalendar calendar)
{
    private static readonly DateTime Min = new(2000, 1, 1), Max = new(2100, 1, 1);

    public Task<bool> IsReadyAsync(CancellationToken ct = default) => leaves.IsReadyAsync(ct);

    /// <summary>Tous les congés d'un agent, du plus récent au plus ancien.</summary>
    public Task<IReadOnlyList<AgentLeave>> GetForAgentAsync(long agentId, CancellationToken ct = default) =>
        leaves.GetLeavesAsync(agentId, Min, Max, ct);

    /// <summary>Congés de tous les agents qui couvrent un jour donné.</summary>
    public Task<IReadOnlyList<AgentLeave>> GetOnDayAsync(DateOnly day, CancellationToken ct = default)
    {
        var from = day.ToDateTime(TimeOnly.MinValue);
        return leaves.GetLeavesAsync(null, from, from.AddDays(1), ct);
    }

    /// <summary>Valide la saisie et cherche les voyages déjà planifiés pendant le congé (sans rien écrire).</summary>
    public async Task<LeaveCheck> CheckAsync(AgentLeaveDraft draft, CancellationToken ct = default)
    {
        var existing = await GetForAgentAsync(draft.AgentId, ct);
        var errors = LeaveRules.Validate(draft, existing, calendar.Today);
        if (errors.Count > 0) return new LeaveCheck(errors, Array.Empty<LeaveConflict>());
        return new LeaveCheck(errors, await leaves.GetConflictsAsync(draft.AgentId, draft.FirstDay, draft.LastDay, ct));
    }

    /// <summary>Enregistre le congé (la validation est rejouée côté serveur).</summary>
    public async Task<LeaveActionResult> AddAsync(AgentLeaveDraft draft, string user, CancellationToken ct = default)
    {
        var errors = LeaveRules.Validate(draft, await GetForAgentAsync(draft.AgentId, ct), calendar.Today);
        if (errors.Count > 0) return new LeaveActionResult(false, string.Join(" ", errors));

        var comment = draft.Comment?.Trim();
        var leave = new AgentLeave(0, draft.AgentId, draft.FirstDay.ToDateTime(TimeOnly.MinValue),
            draft.LastDay.AddDays(1).ToDateTime(TimeOnly.MinValue), draft.Type,
            string.IsNullOrEmpty(comment) ? LeaveTypes.Label(draft.Type) : comment, user, null);
        await leaves.AddAsync(leave, ct);
        return new LeaveActionResult(true, $"{LeaveTypes.Label(draft.Type)} enregistré du {draft.FirstDay:dd/MM/yyyy} au {draft.LastDay:dd/MM/yyyy}.");
    }

    /// <summary>Supprime un congé en cours ou à venir ; un congé terminé n'est jamais modifié.</summary>
    public async Task<LeaveActionResult> DeleteAsync(long leaveId, CancellationToken ct = default)
    {
        var todayStart = calendar.Today.ToDateTime(TimeOnly.MinValue);
        return await leaves.DeleteAsync(leaveId, todayStart, ct)
            ? new LeaveActionResult(true, "Congé supprimé.")
            : new LeaveActionResult(false, "Suppression refusée : congé introuvable ou déjà terminé (journée passée).");
    }
}