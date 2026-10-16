using SmartPlanif.Application.Abstractions;
using SmartPlanif.Application.UseCases;
using Xunit;

namespace SmartPlanif.Domain.Tests;

public class AgentQueryTests
{
    private static readonly AgentListItem[] Agents =
    {
        new(1, "Ali Ben Salah", "علي", "H", 2, 1, 10, "SBIKHA", 5),
        new(2, "Éric Dupont", null, "H", 7, 2, 11, "HAFFOUZ", null),
        new(3, "Sami Trabelsi", null, "H", null, 1, 10, "SBIKHA", null),
    };

    [Fact]
    public void Text_filter_ignores_case_and_accents()
        => Assert.Equal(new long[] { 2 }, AgentQuery.Apply(Agents, new AgentFilter(Text: "eric"), AgentSort.Id, false).Select(a => a.Id));

    [Fact]
    public void Filters_are_combined()
        => Assert.Equal(new long[] { 1 },
            AgentQuery.Apply(Agents, new AgentFilter(CenterId: 1, HasPda: true), AgentSort.Id, false).Select(a => a.Id));

    [Fact]
    public void Sorting_can_be_descending()
        => Assert.Equal(new long[] { 3, 2, 1 }, AgentQuery.Apply(Agents, new AgentFilter(), AgentSort.Id, true).Select(a => a.Id));
}