using SmartPlanif.Application.Abstractions;
using SmartPlanif.Application.UseCases;
using Xunit;

namespace SmartPlanif.Domain.Tests;

public class BusQueryTests
{
    private static readonly DateTime Now = new(2026, 10, 17);

    private static readonly BusListItem[] Buses =
    {
        new(1, "4501 TU 100", 2, "BUS CONFORT", null, 40, 40, new DateTime(2024, 1, 1), 1, 10, "SBIKHA", 0),
        new(2, "4502 TU 100", 1, "BUS STANDARD", null, 30, 50, new DateTime(2012, 1, 1), 2, 11, "HAFFOUZ", 0),
        new(3, "4503 TU 100", 1, "BUS STANDARD", null, 30, 50, null, 1, 10, "SBIKHA", 1),
    };

    [Fact]
    public void Filters_are_combined()
        => Assert.Equal(new long[] { 3 },
            BusQuery.Apply(Buses, new BusFilter(CategoryId: 1, CenterId: 1), BusSort.Id, false, Now).Select(b => b.Id));

    [Fact]
    public void Line_filter_uses_the_bus_id_set()
        => Assert.Equal(new long[] { 1, 2 },
            BusQuery.Apply(Buses, new BusFilter(BusIdsOnLine: new HashSet<long> { 1, 2 }), BusSort.Id, false, Now).Select(b => b.Id));

    [Fact]
    public void Age_bands_are_computed_from_the_service_date()
    {
        Assert.Equal("0-5 ans", BusQuery.AgeBandOf(Buses[0].InServiceSince, Now));
        Assert.Equal("10-15 ans", BusQuery.AgeBandOf(Buses[1].InServiceSince, Now));
        Assert.Equal("Inconnu", BusQuery.AgeBandOf(null, Now));
    }

    [Fact]
    public void Summary_counts_buses_and_seats_per_category()
    {
        var standard = BusQuery.Summarize(Buses, BusGroup.Category, Now).Single(s => s.Label == "BUS STANDARD");
        Assert.Equal(2, standard.Count);
        Assert.Equal(160, standard.Capacity);
    }

    [Fact]
    public void Summary_by_line_counts_each_bus_once()
    {
        var usage = new[] { new BusLineUsage(1, 10, "Ligne 10", 3), new BusLineUsage(1, 10, "Ligne 10", 1), new BusLineUsage(2, 10, "Ligne 10", 2) };
        Assert.Equal(2, BusQuery.SummarizeByLine(Buses, usage).Single().Count);
    }
}