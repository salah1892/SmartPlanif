using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Domain.ValueObjects;

/// <summary>
/// Represents the result of a constraint check, containing any violations found.
/// </summary>
public class ConstraintResult
{
    public bool IsValid => !Violations.Any();
    public List<string> Violations { get; } = new List<string>();

    public void AddViolation(string violation)
    {
        Violations.Add(violation);
    }
}
