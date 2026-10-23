using SmartPlanif.Domain.Constraints;

namespace SmartPlanif.Domain.Allocation;

/// <summary>Contrôle de cohérence d'une politique avant sa sauvegarde ou son utilisation.</summary>
public static class PolicyValidator
{/// <summary>Poids maximal d'un critère souple (borne de saisie et de validation).</summary>
    public const double MaxWeight = 50;
    /// <summary>Part maximale d'un critère dans le total des poids souples de sa famille.</summary>
    public const double MaxFamilyShare = 1.0 / 3;
    /// <summary>Écart maximal entre le plus grand et le plus petit poids souple d'une même famille.</summary>
    public const double MaxWeightRatio = 4;
    /// <summary>Liste des erreurs détectées (vide si la politique est cohérente).</summary>

    private static string FamilyLabel(string family) => family == "bus" ? "Bus" : "Chauffeurs et receveurs";

    public static IReadOnlyList<string> Validate(AllocationPolicy policy)
    {
        var errors = new List<string>();

        if (policy.MaxDailyWork <= TimeSpan.Zero) errors.Add("La durée de travail maximale par jour doit être positive.");
        if (policy.MaxAmplitude < policy.MaxDailyWork) errors.Add("L'amplitude maximale doit être supérieure ou égale à la durée de travail maximale.");
        if (policy.MinAgentRest < TimeSpan.Zero) errors.Add("Le repos minimal d'un agent ne peut pas être négatif.");
        if (policy.MinBusTurnaround < TimeSpan.Zero) errors.Add("Le temps de remise en service d'un bus ne peut pas être négatif.");
        if (policy.MaxTripsPerAgentPerDay < 1) errors.Add("Le nombre maximal de voyages par agent doit être au moins 1.");
        if (policy.MaxBusKmPerDay <= 0) errors.Add("Le kilométrage quotidien maximal d'un bus doit être positif.");
        if (policy.RestSaturation <= TimeSpan.Zero) errors.Add("La durée de saturation du repos doit être positive.");

        // --- Contraintes : plafond, ratios, équilibre ---
        const double maxWeight = 50;
        if (policy.MaxAmplitude < policy.MaxDailyWork) errors.Add("L'amplitude maximale doit être supérieure ou égale à la durée de travail maximale.");
        if (policy.MinAgentRest < TimeSpan.Zero) errors.Add("Le repos minimal d'un agent ne peut pas être négatif.");
        if (policy.MinBusTurnaround < TimeSpan.Zero) errors.Add("Le temps de remise en service d'un bus ne peut pas être négatif.");
        if (policy.MaxTripsPerAgentPerDay < 1) errors.Add("Le nombre maximal de voyages par agent doit être au moins 1.");
        if (policy.MaxBusKmPerDay <= 0) errors.Add("Le kilométrage quotidien maximal d'un bus doit être positif.");
        if (policy.RestSaturation <= TimeSpan.Zero) errors.Add("La durée de saturation du repos doit être positive.");

        // --- Contraintes : plafond, ratios, équilibre ---
        /// Plafond de poids pour les contraintes souples.
        //const double maxWeight = 50;
        //const double maxFamilyShare = 1.0 / 3;
       // const double maxRatio = 4;

        var busWeights = new List<(string Code, double Weight)>();
        var agentWeights = new List<(string Code, double Weight)>();

        foreach (var (code, setting) in policy.Constraints)
        {
            if (setting.Weight < 0) errors.Add($"Le poids de la contrainte {code} ne peut pas être négatif.");

            if (setting.Weight > maxWeight)
                errors.Add($"Le poids de la contrainte {code} ({setting.Weight}) dépasse le plafond de {maxWeight}.");

            if (setting.Mode == ConstraintMode.Soft && setting.Weight == 0)
                errors.Add($"La contrainte {code} est souple avec un poids nul : elle n'aurait aucun effet.");

            // Collecter les poids souples par famille
            if (setting.Mode == ConstraintMode.Soft && setting.Weight > 0)
            {
                var descriptor = ConstraintCatalog.FindDescriptor(code);
                if (descriptor is not null)
                {
                    if (descriptor.Target == ConstraintTarget.Bus)
                        busWeights.Add((code, setting.Weight));
                    else
                        agentWeights.Add((code, setting.Weight));
                }
            }
        }

        foreach (var rule in policy.CategoryRules)
        {
            var notAllowed = rule.PreferredCategoryIds.Where(p => rule.AllowedCategoryIds.Count > 0 && !rule.AllowedCategoryIds.Contains(p));
            if (notAllowed.Any())
                errors.Add($"Type de ligne {rule.LineTypeId} : une catégorie préférée doit aussi être autorisée.");
        }

        // Vérifier la part de chaque critère dans sa famille (max 1/3)
        ValidateFamily(errors, busWeights, "bus", MaxFamilyShare);
        ValidateFamily(errors, agentWeights, "agents", MaxFamilyShare);

        // Vérifier le ratio max/min par famille
        ValidateRatio(errors, busWeights, "bus", MaxWeightRatio);
        ValidateRatio(errors, agentWeights, "agents", MaxWeightRatio);

        // --- ALNS ---
        var alns = policy.Alns;
        if (alns.MaxIterations < 0) errors.Add("ALNS : le nombre d'itérations ne peut pas être négatif.");
        if (alns.MaxSeconds <= 0) errors.Add("ALNS : la durée maximale doit être positive.");
        if (alns.DestroyFractionMin <= 0 || alns.DestroyFractionMax > 1 || alns.DestroyFractionMin > alns.DestroyFractionMax)
            errors.Add("ALNS : la part détruite doit vérifier 0 < min ≤ max ≤ 1.");
        if (alns.InitialTemperature <= 0) errors.Add("ALNS : la température initiale doit être positive.");
        if (alns.CoolingRate <= 0 || alns.CoolingRate >= 1) errors.Add("ALNS : le facteur de refroidissement doit être strictement entre 0 et 1.");
        if (alns.InsertionNoise < 0 || alns.InsertionNoise > 1) errors.Add("ALNS : le bruit d'insertion doit être entre 0 et 1.");
        if (alns.UnassignedPenalty <= 0) errors.Add("ALNS : la pénalité par voyage non affecté doit être positive.");
        if (alns.FairnessWeight < 0) errors.Add("ALNS : le poids de l'équité ne peut pas être négatif.");
        if (alns.ReactionFactor < 0 || alns.ReactionFactor > 1) errors.Add("ALNS : le facteur de réaction doit être entre 0 et 1.");
        if (alns.StagnationLimit < 1) errors.Add("ALNS : la limite de stagnation doit être au moins 1.");
       
        // Vérifier que la somme des poids souples ne domine pas la pénalité d'affectation
        var totalSoftWeight = busWeights.Sum(w => w.Weight) + agentWeights.Sum(w => w.Weight);
        if (totalSoftWeight >= alns.UnassignedPenalty * 0.10)
            errors.Add($"La somme des poids souples ({totalSoftWeight}) doit être inférieure à 10 % de la pénalité par voyage non affecté ({alns.UnassignedPenalty}).");

        return errors;
    }



    private static void ValidateFamily(List<string> errors, List<(string Code, double Weight)> weights, string family, double maxShare)
    {
        // Avec moins de 3 critères souples, la notion de « part dominante » n'a pas de sens.
        if (weights.Count < 3) return;
        var total = weights.Sum(w => w.Weight);
        if (total <= 0) return;

        foreach (var (code, weight) in weights)
        {
            var share = weight / total;
            if (share <= maxShare) continue;
            var allowed = maxShare * (total - weight) / (1 - maxShare);
            errors.Add(
                $"Famille « {FamilyLabel(family)} » : le critère {code} concentre {share * 100:0.#} % du total des poids souples " +
                $"(maximum autorisé : {maxShare * 100:0} %). " +
                $"Correction : descendre {code} à {allowed:0.##} ou moins, ou augmenter les autres poids de la famille.");
        }
    }

    private static void ValidateRatio(List<string> errors, List<(string Code, double Weight)> weights, string family, double maxRatio)
    {
        if (weights.Count < 2) return;
        var min = weights.MinBy(w => w.Weight);
        var max = weights.MaxBy(w => w.Weight);
        if (min.Weight <= 0) return;
        var ratio = max.Weight / min.Weight;
        if (ratio <= maxRatio) return;
        var minNeeded = max.Weight / maxRatio;      // valeur minimale du plus petit poids pour respecter le ratio
        var maxAllowed = min.Weight * maxRatio;     // valeur maximale du plus grand poids pour respecter le ratio
        errors.Add(
            $"Famille « {FamilyLabel(family)} » : écart de poids trop grand entre critères souples — " +
            $"le plus lourd, {max.Code} ({max.Weight:0.##}), pèse {ratio:0.#} fois le plus léger, {min.Code} ({min.Weight:0.##}), " +
            $"alors que l'écart maximal autorisé est ×{maxRatio:0}. " +
            $"Correction : monter {min.Code} à {minNeeded:0.##} ou plus, OU descendre {max.Code} à {maxAllowed:0.##} ou moins.");
    }
}
