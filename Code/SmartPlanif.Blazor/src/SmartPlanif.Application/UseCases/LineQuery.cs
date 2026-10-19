using System.Globalization;
using SmartPlanif.Application.Abstractions;

namespace SmartPlanif.Application.UseCases;

/// <summary>Critères de filtrage des lignes (null = pas de filtre).</summary>
/// <param name="HasTrips">Avec / sans voyages le jour choisi (utilise <paramref name="LineIdsWithTrips"/>).</param>
/// <param name="LineIdsWithTrips">Lignes ayant au moins un voyage le jour choisi (calculé par l'appelant).</param>
public sealed record LineFilter(
    string? Text = null, long? TypeId = null, long? CenterId = null, long? DelegationId = null,
    bool? Active = null, bool? HasTrips = null, IReadOnlySet<long>? LineIdsWithTrips = null);

public enum LineSort { Id, Name, Type, Km, Stops, Center, Delegation }

public enum LineGroup { Type, Center, Delegation }

/// <summary>Un groupe de lignes : clé (pour filtrer), libellé, nombre de lignes et kilomètres cumulés.</summary>
public sealed record LineGroupSummary(string Key, string Label, int Count, double Km);

/// <summary>Filtrage, tri, regroupement et pagination en mémoire (fonctions pures, testables sans base).</summary>
public static class LineQuery
{
    private static bool Contains(string? haystack, string needle) =>
        haystack is not null &&
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    public static IReadOnlyList<LineListItem> Apply(
        IEnumerable<LineListItem> source, LineFilter f, LineSort sort, bool descending)
    {
        var query = source;
        var text = f.Text?.Trim();
        if (!string.IsNullOrEmpty(text))
            query = query.Where(l => Contains(l.Id.ToString(), text) || Contains(l.NameFr, text) || Contains(l.NameAr, text));
        if (f.TypeId is { } t) query = query.Where(l => l.TypeId == t);
        if (f.CenterId is { } c) query = query.Where(l => l.CenterId == c);
        if (f.DelegationId is { } d) query = query.Where(l => l.DelegationId == d);
        if (f.Active is { } a) query = query.Where(l => l.Active == a);
        if (f.HasTrips is { } h && f.LineIdsWithTrips is { } ids) query = query.Where(l => ids.Contains(l.Id) == h);

        Func<LineListItem, object?> key = sort switch
        {
            LineSort.Name => l => l.NameFr ?? "",
            LineSort.Type => l => l.TypeLabel ?? "",
            LineSort.Km => l => l.Km ?? double.MaxValue,
            LineSort.Stops => l => l.Stops,
            LineSort.Center => l => l.CenterId ?? long.MaxValue,
            LineSort.Delegation => l => l.DelegationLabel ?? "",
            _ => l => l.Id,
        };
        var ordered = descending ? query.OrderByDescending(key) : query.OrderBy(key);
        return ordered.ThenBy(l => l.Id).ToList();
    }

    /// <summary>Regroupe les lignes et compte lignes et kilomètres par groupe.</summary>
    public static IReadOnlyList<LineGroupSummary> Summarize(IEnumerable<LineListItem> lines, LineGroup group)
    {
        Func<LineListItem, (string Key, string Label)> pick = group switch
        {
            LineGroup.Type => l => (l.TypeId?.ToString() ?? "", l.TypeLabel ?? "Sans type"),
            LineGroup.Center => l => (l.CenterId?.ToString() ?? "", l.CenterId is { } c ? $"Centre {c}" : "Sans centre"),
            _ => l => (l.DelegationId?.ToString() ?? "", l.DelegationLabel ?? (l.DelegationId is { } d ? $"Délégation {d}" : "Sans délégation")),
        };
        return lines.GroupBy(pick)
            .Select(g => new LineGroupSummary(g.Key.Key, g.Key.Label, g.Count(), g.Sum(l => l.Km ?? 0)))
            .OrderBy(s => s.Label).ToList();
    }

    public static IReadOnlyList<T> Page<T>(IReadOnlyList<T> items, int page, int pageSize) =>
        items.Skip((Math.Max(1, page) - 1) * pageSize).Take(pageSize).ToList();

    public static int PageCount(int total, int pageSize) => Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
}

/// <summary>Cas d'usage « consulter les lignes ».</summary>
public sealed class LineQueryService(ILineRepository lines)
{
    public Task<IReadOnlyList<LineListItem>> LoadAsync(CancellationToken ct = default) => lines.GetLinesAsync(ct);

    public Task<IReadOnlyList<LineDayStats>> GetDayStatsAsync(DateOnly date, CancellationToken ct = default) =>
        lines.GetDayStatsAsync(date, ct);

    public Task<LineItinerary> GetItineraryAsync(long lineId, CancellationToken ct = default) =>
        lines.GetItineraryAsync(lineId, ct);

    public Task<IReadOnlyList<LineTripRow>> GetTripsAsync(long lineId, DateOnly date, CancellationToken ct = default) =>
        lines.GetLineTripsAsync(lineId, date, ct);
}