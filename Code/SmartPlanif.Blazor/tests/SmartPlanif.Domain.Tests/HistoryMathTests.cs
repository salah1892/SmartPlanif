using SmartPlanif.Application.Abstractions;
using SmartPlanif.Application.UseCases;
using Xunit;

namespace SmartPlanif.Domain.Tests;

public class HistoryMathTests
{
    private static DayQuality Day(DateOnly d, int trips, int planned, int buses, int agents) =>
        new(d, trips, planned, 0, buses, agents, 0, 0, null, 0, 0);

    [Fact]
    public void Granularity_is_chosen_from_the_length_of_the_period()
    {
        var to = new DateOnly(2026, 10, 18);
        Assert.Equal(HistoryGranularity.Day, HistoryMath.Resolve(to.AddDays(-29), to, HistoryGranularity.Auto));
        Assert.Equal(HistoryGranularity.Week, HistoryMath.Resolve(to.AddDays(-89), to, HistoryGranularity.Auto));
        Assert.Equal(HistoryGranularity.Month, HistoryMath.Resolve(to.AddDays(-364), to, HistoryGranularity.Auto));
        Assert.Equal(HistoryGranularity.Day, HistoryMath.Resolve(to.AddDays(-364), to, HistoryGranularity.Day));
    }

    [Fact]
    public void Weeks_start_on_monday()
        => Assert.Equal(new DateOnly(2026, 10, 12), HistoryMath.KeyOf(new DateOnly(2026, 10, 18), HistoryGranularity.Week));

    [Fact]
    public void Rates_are_weighted_by_trips_and_utilization_is_averaged_per_day()
    {
        var days = new[]
        {
            Day(new DateOnly(2026, 10, 1), 100, 100, 10, 20),   // 100 %, bus 50 %, agents 40 %
            Day(new DateOnly(2026, 10, 2), 300, 150, 20, 30),   // 50 %,  bus 100 %, agents 60 %
        };
        var p = HistoryMath.Aggregate(days, 20, 50, HistoryGranularity.Month).Single();

        Assert.Equal(400, p.Trips);
        Assert.Equal(62.5, p.CoverageRate!.Value, 6);          // 250 / 400
        Assert.Equal(75, p.BusUtilization!.Value, 6);          // (50 + 100) / 2
        Assert.Equal(50, p.AgentUtilization!.Value, 6);        // (40 + 60) / 2
        Assert.Null(p.OnTimeRate);                             // aucun retard mesuré
    }

    [Fact]
    public void Resource_series_fills_missing_days_with_zero()
    {
        var rows = new[] { new ResourceDay(new DateOnly(2026, 10, 2), 3) };
        var (labels, values) = HistoryMath.ResourceSeries(rows, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 3), HistoryGranularity.Day);

        Assert.Equal(3, labels.Count);
        Assert.Equal(new double?[] { 0, 3, 0 }, values);
    }
}
