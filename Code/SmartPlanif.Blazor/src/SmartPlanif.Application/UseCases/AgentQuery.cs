namespace SmartPlanif.Application.UseCases;

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using SmartPlanif.Application.Abstractions;


/// <summary>Critères de filtrage de la liste des agents (null = pas de filtre).</summary>
public sealed record AgentFilter(
    string? Text = null, long? CenterId = null, long? DelegationId = null,
    int? RestDay = null, string? Qualification = null, bool? HasPda = null);

/// <summary>Colonne de tri.</summary>
public enum AgentSort { Id, Name, Center, Delegation, RestDay }

/// <summary>Filtrage, tri et pagination en mémoire (fonctions pures, testables sans base).</summary>
public static class AgentQuery
{
    private static bool Contains(string? haystack, string needle) =>
        haystack is not null &&
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    public static IReadOnlyList<AgentListItem> Apply(
        IEnumerable<AgentListItem> source, AgentFilter f, AgentSort sort, bool descending)
    {
        var query = source;
        var text = f.Text?.Trim();
        if (!string.IsNullOrEmpty(text))
            query = query.Where(a => a.Id.ToString() == text || Contains(a.Id.ToString(), text)
                                     || Contains(a.NameFr, text) || Contains(a.NameAr, text));
        if (f.CenterId is { } c) query = query.Where(a => a.CenterId == c);
        if (f.DelegationId is { } d) query = query.Where(a => a.DelegationId == d);
        if (f.RestDay is { } r) query = query.Where(a => a.RestDay == r);
        if (!string.IsNullOrEmpty(f.Qualification)) query = query.Where(a => a.Qualification == f.Qualification);
        if (f.HasPda is { } hasPda) query = query.Where(a => (a.Pda is > 0) == hasPda);

        Func<AgentListItem, object?> key = sort switch
        {
            AgentSort.Name => a => a.NameFr ?? "",
            AgentSort.Center => a => a.CenterId ?? long.MaxValue,
            AgentSort.Delegation => a => a.DelegationLabel ?? "",
            AgentSort.RestDay => a => a.RestDay ?? int.MaxValue,
            _ => a => a.Id,
        };
        var ordered = descending ? query.OrderByDescending(key) : query.OrderBy(key);
        return ordered.ThenBy(a => a.Id).ToList();
    }

    public static IReadOnlyList<T> Page<T>(IReadOnlyList<T> items, int page, int pageSize) =>
        items.Skip((Math.Max(1, page) - 1) * pageSize).Take(pageSize).ToList();

    public static int PageCount(int total, int pageSize) => Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
}

/// <summary>Cas d'usage « gérer / consulter les agents ».</summary>
public sealed class AgentQueryService(IAgentRepository agents)
{
    public Task<IReadOnlyList<AgentListItem>> LoadAsync(CancellationToken ct = default) => agents.GetAgentsAsync(ct);

    public Task<IReadOnlyList<AgentTripRow>> GetTripsAsync(long agentId, DateOnly date, CancellationToken ct = default) =>
        agents.GetAgentTripsAsync(agentId, date, ct);
}