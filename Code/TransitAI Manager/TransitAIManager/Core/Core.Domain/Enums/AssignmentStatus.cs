using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Domain.Enums;

/// <summary>
/// Represents the status of a trip assignment
/// </summary>
public enum AssignmentStatus
{
    Proposed = 0,
    Validated = 1,
    Modified = 2,
    Cancelled = 3,
    Realized = 4,
    Replaced = 5
}
