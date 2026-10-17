using SmartPlanif.Application.Abstractions;
using SmartPlanif.Application.UseCases;
using Xunit;

namespace SmartPlanif.Domain.Tests;

public class LeaveRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 17);
    private static AgentLeaveDraft Draft(DateOnly a, DateOnly b, string type = "CONGE") => new(1, type, a, b, null);

    private static AgentLeave Existing(DateOnly a, DateOnly b) =>
        new(1, 1, a.ToDateTime(TimeOnly.MinValue), b.AddDays(1).ToDateTime(TimeOnly.MinValue), "CONGE", null, null, null);

    [Fact]
    public void A_valid_future_leave_has_no_error()
        => Assert.Empty(LeaveRules.Validate(Draft(Today, Today.AddDays(4)), Array.Empty<AgentLeave>(), Today));

    [Fact]
    public void End_before_start_is_rejected()
        => Assert.NotEmpty(LeaveRules.Validate(Draft(Today.AddDays(3), Today), Array.Empty<AgentLeave>(), Today));

    [Fact]
    public void A_fully_past_leave_is_rejected_but_an_ongoing_one_is_accepted()
    {
        Assert.NotEmpty(LeaveRules.Validate(Draft(Today.AddDays(-5), Today.AddDays(-1)), Array.Empty<AgentLeave>(), Today));
        Assert.Empty(LeaveRules.Validate(Draft(Today.AddDays(-2), Today.AddDays(2)), Array.Empty<AgentLeave>(), Today));
    }

    [Fact]
    public void Overlapping_an_existing_leave_is_rejected_but_adjacent_days_are_fine()
    {
        var existing = new[] { Existing(Today.AddDays(2), Today.AddDays(5)) };
        Assert.NotEmpty(LeaveRules.Validate(Draft(Today.AddDays(5), Today.AddDays(7)), existing, Today));
        Assert.Empty(LeaveRules.Validate(Draft(Today.AddDays(6), Today.AddDays(8)), existing, Today));
    }

    [Fact]
    public void Unknown_type_is_rejected()
        => Assert.NotEmpty(LeaveRules.Validate(Draft(Today, Today, "XYZ"), Array.Empty<AgentLeave>(), Today));
}