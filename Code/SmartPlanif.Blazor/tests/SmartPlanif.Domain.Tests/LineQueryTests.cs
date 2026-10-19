using SmartPlanif.Application.Abstractions;
using SmartPlanif.Application.UseCases;
using Xunit;

namespace SmartPlanif.Domain.Tests;

public class LineQueryTests
{
    private static readonly LineListItem[] Lines =
    {
        new(10, "Ligne 10 Centre-Gare", "خط", 1, "URBAIN", null, 22, 1, 10, "SBIKHA", null, null, true, true, 12),
        new(13, "Kairouan-Tunis", null, 2, "INTERURBAIN", null, 160, 2, 11, "HAFFOUZ", null, null, true, false, 20),
        new(99, "Ancienne ligne", null, 1, "URBAIN", null, null, 1, 10, "SBIKHA", null, null, false, false, 0),
    };

    [Fact]
    public void Text_filter_ignores_case_and_accents()
        => Assert.Equal(new long[] { 10 }, LineQuery.Apply(Lines, new LineFilter(Text: "CENTRE"), LineSort.Id, false).Select(l => l.Id));

    [Fact]
    public void Filters_are_combined()
        => Assert.Equal(new long[] { 99 },
            LineQuery.Apply(Lines, new LineFilter(TypeId: 1, Active: false), LineSort.Id, false).Select(l => l.Id));

    [Fact]
    public void Trips_filter_uses_the_line_id_set()
    {
        var ids = new HashSet<long> { 10, 13 };
        Assert.Equal(new long[] { 99 },
            LineQuery.Apply(Lines, new LineFilter(HasTrips: false, LineIdsWithTrips: ids), LineSort.Id, false).Select(l => l.Id));
        Assert.Equal(new long[] { 10, 13 },
            LineQuery.Apply(Lines, new LineFilter(HasTrips: true, LineIdsWithTrips: ids), LineSort.Id, false).Select(l => l.Id));
    }

    [Fact]
    public void Lines_without_km_come_last_when_sorting_by_km()
        => Assert.Equal(new long[] { 10, 13, 99 }, LineQuery.Apply(Lines, new LineFilter(), LineSort.Km, false).Select(l => l.Id));

    [Fact]
    public void Summary_counts_lines_and_km_per_type()
    {
        var urban = LineQuery.Summarize(Lines, LineGroup.Type).Single(s => s.Label == "URBAIN");
        Assert.Equal(2, urban.Count);
        Assert.Equal(22, urban.Km);
    }
}