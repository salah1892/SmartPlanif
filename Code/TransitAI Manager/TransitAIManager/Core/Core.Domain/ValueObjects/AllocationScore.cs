using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Domain.ValueObjects;

/// <summary>
/// Represents the calculated score of an allocation based on soft constraints.
/// </summary>
public class AllocationScore
{
    public int BusScore { get; set; }
    public int DriverScore { get; set; }
    public int ReceiverScore { get; set; }
    public int Penalties { get; set; }

    public int TotalScore => BusScore + DriverScore + ReceiverScore - Penalties;
}

