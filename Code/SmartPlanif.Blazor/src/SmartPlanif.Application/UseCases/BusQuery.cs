using System.Globalization;
using SmartPlanif.Application.Abstractions;

namespace SmartPlanif.Application.UseCases;

/// <summary>Critères de filtrage des bus (null = pas de filtre).</summary>
/// <param name="BusIdsOnLine">Bus desservant la ligne choisie (calculé par l'appelant à partir de BusLineUsage).</param>
public sealed record BusFilter(
    string? Text = null, long? CategoryId = null, long? CenterId = null, long? DelegationId = null,
    string? AgeBand = null, bool? Active = null, IReadOnlySet<long>? BusIdsOnLine = null);

/// <summary>Colonne de tri.</summary>
public enum BusSort { Id, Plate, Category, Center, Delegation, Capacity, Age }

/// <summary>Critère de regroupement.</summary>
public enum BusGroup { Category, Center, Delegation, Line, AgeBand }

/// <summary>Un groupe de bus : clé (pour filtrer), libellé, nombre de bus et capacité cumulée.</summary>
public sealed record BusGroupSummary(string Key, string Label, int Count, int Capacity);

/// <summary>Filtrage, tri, regroupement et pagination en mémoire (fonctions pures, testables sans base).</summary>
public static class BusQuery
{
    private static bool Contains(string? haystack, string needle) =>
        haystack is not null &&
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    /// <summary>Tranche d'âge d'un bus d'après sa date de mise en circulation.</summary>
    public static string AgeBandOf(DateTime? since, DateTime now)
    {
        if (since is not { } s) return "Inconnu";
        var years = (now - s).TotalDays / 365.25;
        return years < 5 ? "0-5 ans" : years < 10 ? "5-10 ans" : years < 15 ? "10-15 ans" : "15 ans et +";
    }

    public static IReadOnlyList<BusListItem> Apply(
        IEnumerable<BusListItem> source, BusFilter f, BusSort sort, bool descending, DateTime now)
    {
        var query = source;
        var text = f.Text?.Trim();
        if (!string.IsNullOrEmpty(text))
            query = query.Where(b => Contains(b.Plate, text) || Contains(b.Id.ToString(), text));
        if (f.CategoryId is { } cat) query = query.Where(b => b.CategoryId == cat);
        if (f.CenterId is { } c) query = query.Where(b => b.CenterId == c);
        if (f.DelegationId is { } d) query = query.Where(b => b.DelegationId == d);
        if (!string.IsNullOrEmpty(f.AgeBand)) query = query.Where(b => AgeBandOf(b.InServiceSince, now) == f.AgeBand);
        if (f.Active is { } active) query = query.Where(b => (b.ActiveFlag == 1) == active);
        if (f.BusIdsOnLine is { } ids) query = query.Where(b => ids.Contains(b.Id));

        Func<BusListItem, object?> key = sort switch
        {
            BusSort.Plate => b => b.Plate ?? "",
            BusSort.Category => b => b.CategoryLabel ?? "",
            BusSort.Center => b => b.CenterId ?? long.MaxValue,
            BusSort.Delegation => b => b.DelegationLabel ?? "",
            BusSort.Capacity => b => b.Capacity,
            BusSort.Age => b => b.InServiceSince ?? DateTime.MaxValue,   // croissant = du plus ancien au plus récent
            _ => b => b.Id,
        };
        var ordered = descending ? query.OrderByDescending(key) : query.OrderBy(key);
        return ordered.ThenBy(b => b.Id).ToList();
    }

    /// <summary>Regroupe les bus (hors ligne) et compte bus et places par groupe.</summary>
    public static IReadOnlyList<BusGroupSummary> Summarize(IEnumerable<BusListItem> buses, BusGroup group, DateTime now)
    {
        Func<BusListItem, (string Key, string Label)> pick = group switch
        {
            BusGroup.Category => b => (b.CategoryId?.ToString() ?? "", b.CategoryLabel ?? "Sans catégorie"),
            BusGroup.Center => b => (b.CenterId?.ToString() ?? "", b.CenterId is { } c ? $"Centre {c}" : "Sans centre"),
            BusGroup.Delegation => b => (b.DelegationId?.ToString() ?? "", b.DelegationLabel ?? (b.DelegationId is { } d ? $"Délégation {d}" : "Sans délégation")),
            _ => b => { var band = AgeBandOf(b.InServiceSince, now); return (band, band); },
        };
        return buses.GroupBy(pick)
            .Select(g => new BusGroupSummary(g.Key.Key, g.Key.Label, g.Count(), g.Sum(b => b.Capacity)))
            .OrderBy(s => s.Label).ToList();
    }

    /// <summary>Regroupement par ligne desservie le jour choisi (un bus peut figurer dans plusieurs lignes).</summary>
    public static IReadOnlyList<BusGroupSummary> SummarizeByLine(IEnumerable<BusListItem> buses, IEnumerable<BusLineUsage> usage)
    {
        var byId = buses.ToDictionary(b => b.Id);
        return usage.Where(u => byId.ContainsKey(u.BusId))
            .GroupBy(u => (u.LineId, u.LineLabel))
            .Select(g => new BusGroupSummary(g.Key.LineId.ToString(), g.Key.LineLabel,
                g.Select(u => u.BusId).Distinct().Count(), g.Select(u => u.BusId).Distinct().Sum(id => byId[id].Capacity)))
            .OrderBy(s => s.Label).ToList();
    }

    public static IReadOnlyList<T> Page<T>(IReadOnlyList<T> items, int page, int pageSize) =>
        items.Skip((Math.Max(1, page) - 1) * pageSize).Take(pageSize).ToList();

    public static int PageCount(int total, int pageSize) => Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
}

/// <summary>Cas d'usage « consulter les bus ».</summary>
public sealed class BusQueryService(IBusRepository buses)
{
    public Task<IReadOnlyList<BusListItem>> LoadAsync(CancellationToken ct = default) => buses.GetBusesAsync(ct);

    public Task<IReadOnlyList<BusLineUsage>> GetLineUsageAsync(DateOnly date, CancellationToken ct = default) =>
        buses.GetBusLineUsageAsync(date, ct);

    public Task<IReadOnlyList<BusTripRow>> GetTripsAsync(long busId, DateOnly date, CancellationToken ct = default) =>
        buses.GetBusTripsAsync(busId, date, ct);
}