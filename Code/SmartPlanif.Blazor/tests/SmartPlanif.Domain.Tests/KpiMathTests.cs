using SmartPlanif.Application.Abstractions;
using SmartPlanif.Application.UseCases;
using Xunit;

namespace SmartPlanif.Domain.Tests;

public class KpiMathTests
{
    private static readonly DateOnly D = new(2026, 10, 17);

    [Fact]
    public void Day_status_follows_the_coverage()
    {
        Assert.Equal(DayStatus.Empty, KpiMath.StatusOf(new DayCoverage(D, 0, 0, 0, 0)));
        Assert.Equal(DayStatus.Unplanned, KpiMath.StatusOf(new DayCoverage(D, 10, 0, 0, 0)));
        Assert.Equal(DayStatus.Partial, KpiMath.StatusOf(new DayCoverage(D, 10, 6, 2, 0)));
        Assert.Equal(DayStatus.Partial, KpiMath.StatusOf(new DayCoverage(D, 10, 0, 3, 0)));
        Assert.Equal(DayStatus.Complete, KpiMath.StatusOf(new DayCoverage(D, 10, 10, 0, 0)));
    }

    [Fact]
    public void Strict_rate_ignores_missing_real_while_literal_rate_counts_it()
    {
        // 100 prévus, 20 sans réel, 10 modifiés : strict = 10/80, littéral = 30/100 (définition du chapitre 7).
        var role = new RoleChange(Planned: 100, Changed: 10, NoReal: 20);
        Assert.Equal(0.125, role.Strict!.Value, 6);
        Assert.Equal(0.30, role.Literal!.Value, 6);
    }

    [Fact]
    public void Rates_are_undefined_without_data()
    {
        var empty = new RoleChange(0, 0, 0);
        Assert.Null(empty.Strict);
        Assert.Null(empty.Literal);
        Assert.Null(new RoleChange(5, 0, 5).Strict);   // aucun réel connu : taux strict indéfini
    }
}