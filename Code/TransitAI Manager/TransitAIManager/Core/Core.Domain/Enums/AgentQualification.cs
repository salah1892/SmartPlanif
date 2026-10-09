using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Domain.Enums;

/// <summary>
/// Represents the qualification of an agent (Chauffeur vs Receveur)
/// </summary>
public enum AgentQualification
{
    Chauffeur = 1,
    Receveur = 2,
    Other = 99
}
