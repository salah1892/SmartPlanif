using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Domain.ValueObjects;

/// <summary>
/// Represents a time slot with start and end times.
/// </summary>
public class TimeSlot
{
    public TimeSpan Start { get; }
    public TimeSpan End { get; }

    public TimeSlot(TimeSpan start, TimeSpan end)
    {
        if (start > end)
            throw new ArgumentException("Start time cannot be after end time.");
        Start = start;
        End = end;
    }

    public bool OverlapsWith(TimeSlot other)
    {
        return Start < other.End && other.Start < End;
    }
}

