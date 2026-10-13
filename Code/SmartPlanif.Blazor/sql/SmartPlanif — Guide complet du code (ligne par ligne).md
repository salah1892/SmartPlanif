# SmartPlanif — Guide complet du code, ligne par ligne

**Objectif de ce guide** : vous permettre de comprendre chaque fichier de l'archive `SmartPlanif.Blazor.zip`, puis de le modifier et de l'intégrer à votre propre solution.

## 0. Comment lire ce guide

- Les fichiers sont expliqués **dans l'ordre logique** : Domain, puis Application, Infrastructure, Web, Tests.
- Pour chaque fichier : son **rôle**, puis un tableau « instruction → explication ». Les instructions sont citées telles quelles (les numéros de ligne changent à chaque modification, donc je ne les utilise pas).
- Les éléments qui se répètent partout (commentaires `///`, accolades, `using`, `namespace`) sont expliqués **une seule fois** dans la section 2. Les lignes quasi identiques (par exemple les 14 contraintes du catalogue) sont expliquées par modèle, puis par différences.
- Chaque partie se termine par **« Pour modifier »** : ce qu'il faut toucher selon le besoin.
- Une phrase en **gras** signale un point où l'on se trompe facilement.

**À propos de votre dépôt GitHub** : je n'ai pas pu lire `https://github.com/salah1892/SmartPlanif` (il est probablement privé ou non indexé). Le guide d'intégration de la dernière partie repose donc sur des hypothèses raisonnables. Si vous m'envoyez l'arborescence de votre dépôt (liste des dossiers et projets), je l'adapterai précisément.

## 1. Vue d'ensemble : le trajet d'une allocation

Quand vous cliquez sur « Lancer l'allocation » dans la page `Allocation.razor` :

1. La page crée une commande `AllocateDayCommand` (date, utilisateur, méthode, enregistrer ou non) et appelle `AllocateDayHandler.HandleAsync`.
2. Le handler **charge les données une seule fois** : politique (réglages), voyages à affecter, voyages déjà affectés, bus, agents, indisponibilités, libellés des types de ligne.
3. Sur un fil d'arrière-plan (`Task.Run`), il exécute `Compute` :
   - il construit un `ScheduleBook` (les plannings de toutes les ressources) et y inscrit les voyages déjà affectés ;
   - il calcule le **diagnostic de faisabilité** ;
   - il **construit** un premier plan (glouton chronologique OU construction par phases) ;
   - si la méthode le demande, il **améliore** ce plan avec l'ALNS ;
   - il lance une **passe de complétion** pour les voyages restants ;
   - il recalcule les **scores exacts**, lance le **contrôle indépendant** et calcule les **causes d'échec**.
4. Si l'enregistrement est demandé **et** si le plan n'a aucune violation, il écrit les propositions via `IAssignmentRepository`.
5. La page affiche le rapport (`AllocationReport`).

Pour **un voyage**, la décision se prend dans `TripAllocator.TryAssign` :

```
Tous les bus      -> règles DURES (élimination) -> règles SOUPLES (score) -> classés -> le meilleur
Tous les chauffeurs   (même mécanique)
Tous les receveurs    (même mécanique, receveur différent du chauffeur)
```

Les **règles** appliquées ne sont pas fixes : elles viennent du `ConstraintCatalog` selon les choix de l'utilisateur (désactivée, dure, souple).

## 2. Lexique C# utilisé partout (à lire une fois)

| Élément | Signification |
| --- | --- |
| `namespace X;` | Range le fichier dans un « dossier logique » nommé X. La forme avec point-virgule s'applique à tout le fichier. |
| `using X;` | Permet d'écrire les types de X sans préfixe. `using static X.Y;` fait de même pour les membres statiques de Y. |
| `///` | Commentaire de documentation XML : affiché par l'IDE au survol. N'a aucun effet à l'exécution. |
| `<inheritdoc />` | « La documentation est celle de l'interface » (évite de la recopier). |
| `public / private / internal` | Visibilité : partout / dans la classe / dans le projet. |
| `sealed` | La classe ne peut pas être héritée (plus sûr, légèrement plus rapide). |
| `record` | Type de **données immuables** : on déclare les champs entre parenthèses, C# génère constructeur, propriétés, égalité par valeur. |
| `record struct` | Même chose mais type valeur (copié, pas de référence). |
| `a with { Score = 5 }` | Crée une **copie** du record `a` avec seulement `Score` modifié. |
| `class X(string code)` | **Constructeur principal** (C# 12) : `code` est utilisable dans toute la classe. |
| `{ get; set; }` | Propriété lisible et modifiable. `{ get; }` : lecture seule. |
| `=> expression` | Corps de méthode ou propriété en une expression. |
| `long?`, `string?` | Type **nullable** : la valeur peut être absente (`null`). `long` = entier 64 bits (colonne `bigint`). |
| `x is null` / `x is not null` | Test d'absence / de présence. |
| `x is { } y` | « Si x n'est pas null, appelle-le y » (motif de décomposition). |
| `var` | Le compilateur déduit le type. |
| `new(...)` | Création où le type est déduit du contexte. |
| `IReadOnlyList<T>` | Liste qu'on peut lire mais pas modifier. |
| `Dictionary<K,V>` | Table clé -> valeur. `dict.GetValueOrDefault(k)` renvoie la valeur ou 0/null. |
| `(a, b)` | Tuple : deux valeurs regroupées. `var (x, y) = t;` les sépare (déconstruction). |
| `<T>` | Générique : le même code fonctionne pour plusieurs types (ici `Bus` et `Agent`). |
| `Func<A,B>` | Une fonction passée en paramètre. `x => x.Id` est une fonction anonyme (lambda). |
| LINQ | Requêtes sur collections : `Where` (filtrer), `Select` (transformer), `OrderBy/ThenBy` (trier), `GroupBy` (regrouper), `Any/Count/Sum/Min/Max/Average`, `First/FirstOrDefault`, `Take(n)`, `Concat`, `Distinct`, `ToList()` (matérialiser). |
| `foreach (var (k, v) in dict)` | Parcours d'un dictionnaire avec déconstruction clé/valeur. |
| `try { } finally { }` | Le bloc `finally` s'exécute **toujours**, même après une erreur ou un `return`. |
| `lock (obj) { }` | Un seul fil à la fois dans le bloc (protège un fichier ou une liste). |
| `async / await / Task` | Appels asynchrones : l'interface ne se bloque pas pendant la lecture d'une base. |
| `CancellationToken` | Signal d'annulation (bouton Arrêter). |
| `IProgress<T>` | Canal pour envoyer une progression à l'appelant. |
| `Random(seed)` | Générateur pseudo-aléatoire : **même graine = même suite** (résultats reproductibles). |
| Injection de dépendances (DI) | Les classes déclarent ce dont elles ont besoin (interfaces) ; `Program.cs` choisit les implémentations. |
| Razor | Mélange de HTML et C# : `@page` (adresse), `@inject` (injection), `@bind` (liaison champ/variable), `@onclick`, `@code { }` (code de la page), `@if`, `@foreach`. |

## 3. Arborescence de la solution

```
SmartPlanif.sln
sql/001_allocation_tables.sql            tables à ajouter à la base
src/SmartPlanif.Domain/                  le cerveau (aucune dépendance externe)
  Common/TimeInterval.cs
  Entities/Entities.cs, ResourcePool.cs
  Scheduling/ResourceSchedule.cs, ScheduleBook.cs
  Constraints/ConstraintModels.cs, ConstraintCatalog.cs
  Rules/Contracts.cs, Hard/*.cs, Soft/SoftRules.cs
  Allocation/ (AllocationPolicy, CandidateSelector, TripAllocator, AllocationState,
               ObjectiveFunction, PlanEvaluator, PlanValidator, FeasibilityAnalyzer,
               PolicyValidator, ConstructionStrategies, PlanCompleter, JustificationFormatter)
  Alns/ (AlnsModels, Operators, AlnsImprover)
src/SmartPlanif.Application/             cas d'usage et interfaces (ports)
src/SmartPlanif.Infrastructure/          MySQL, données de démo, fichier JSON de réglages
src/SmartPlanif.Web/                     interface Blazor
tests/SmartPlanif.Domain.Tests/          tests xunit
```

**Règle de dépendance** : `Web -> Infrastructure -> Application -> Domain`. Le Domain ne connaît personne. C'est ce qui permet de changer la base ou l'interface sans toucher aux règles.

---

# PARTIE A — DOMAIN : les données

## A1. `Common/TimeInterval.cs`

**Rôle** : représenter une période `[début, fin[` et dire si deux périodes se chevauchent. Tout test de conflit du projet passe par ici.

| Instruction | Explication |
| --- | --- |
| `public readonly record struct TimeInterval(DateTime Start, DateTime End)` | Type valeur immuable à deux champs. On utilise des `DateTime` **complets** (date + heure), jamais des heures seules : un voyage après minuit reste correct. |
| `public TimeSpan Duration => End - Start;` | Durée de l'intervalle (soustraction de deux dates = `TimeSpan`). |
| `public bool Overlaps(TimeInterval other) => Start < other.End && End > other.Start;` | Règle du document de conception : A chevauche B si **DébutA < FinB ET FinA > DébutB**. Deux voyages bout à bout (fin de A = début de B) ne se chevauchent **pas**, car `End > other.Start` est faux. |

**Exemple** : A = 06:00–08:00, B = 07:00–09:00 -> `6<9` et `8>7` -> chevauchement. A = 06:00–08:00, B = 08:00–10:00 -> `8>8` faux -> pas de chevauchement.

**Pour modifier** : si l'entreprise exige un battement minimal, ne touchez pas à `Overlaps` : c'est le rôle de la règle `MinGapRule` (partie C).

## A2. `Entities/Entities.cs`

**Rôle** : les objets métier. Ils ne contiennent aucune logique d'accès aux données : c'est la couche Infrastructure qui traduit vos tables en ces objets.

| Instruction | Explication |
| --- | --- |
| `public enum AgentRole { Driver, Receiver, Other }` | Les trois rôles. `Other` = agent qui n'est ni chauffeur ni receveur (ou rôle non reconnu). |
| `public enum ResourceKind { Bus, Agent }` | Sert à retrouver les indisponibilités (congé d'un agent, maintenance d'un bus). |
| `public sealed record Agent(long Id, string Name, AgentRole Role, DayOfWeek? RestDay, long? CenterId, long? DelegationId);` | Un agent. `Id` = `dragent.DECAGEN`. `Name` = `DENAGEN`. `Role` est **déduit** de `DECQUAL` par un mapping configurable. `RestDay` vient de `DECJOUR`. `CenterId`/`DelegationId` = `DECCENT`/`DECDELEG` (nullable car la base les autorise vides). |
| `public sealed record Bus(long Id, string Plate, bool Active, int Seats, int Standing, long? CategoryId, long? CenterId, long? DelegationId, DateTime? InServiceSince)` | Un bus (table `drvehic`). `Plate` = `DEMATRI`. `Active` = `DEACTIF = 1`. `Seats`/`Standing` = `DENBRPA`/`DENBRPD`. `CategoryId` = `DECATVH` (standard, confort...). `InServiceSince` = `DEDATEC`. |
| `{ public int Capacity => Seats + Standing; }` | Propriété calculée : capacité totale (formule du document). Ajoutée dans le corps du record. |
| `public sealed record Trip(long Id, long LineId, string LineLabel, long? LineTypeId, DateTime Departure, DateTime Arrival, double DistanceKm, long? CenterId, long? DelegationId, long? OriginStationId, long? DestinationStationId, int? ExpectedPassengers)` | Un voyage. `Id` = `trips.ID`. `LineTypeId` = `drligne.DECTYLI` (urbain, interurbain). `Arrival` **n'existe pas dans `trips`** : elle est calculée depuis `stop_times` par le dépôt. `DistanceKm` sert aux règles B11 et BP06. `CenterId`/`DelegationId` sont ceux de la **ligne**. `Origin`/`DestinationStationId` servent à BP05 (continuité de station). `ExpectedPassengers` est `null` aujourd'hui (donnée absente de la base). |
| `public TimeInterval Interval => new(Departure, Arrival);` | Intervalle d'occupation des ressources par ce voyage. |
| `public DateOnly WorkDate => DateOnly.FromDateTime(Departure);` | Jour de rattachement (jour du départ) : sert à tous les cumuls journaliers (8 h, nombre de voyages, km). |
| `public sealed record Unavailability(ResourceKind Kind, long ResourceId, TimeInterval Period, string Reason);` | Une période d'indisponibilité : quel type de ressource, laquelle, quand, pourquoi. |
| `public sealed record AssignedTrip(Trip Trip, long BusId, long DriverId, long ReceiverId);` | Un voyage **déjà affecté** avant cette exécution : il occupe ses ressources mais n'est pas replanifié. |

**Points d'attention**

- **`DayOfWeek` .NET : dimanche = 0, lundi = 1... samedi = 6.** Votre base : lundi = 1... dimanche = 7. La conversion est un modulo 7 dans `MySqlResourceRepository.ToDayOfWeek`.
- Les `record` sont **immuables** : pour « modifier » un voyage on en crée un nouveau. C'est ce qui rend les copies de plannings sûres (partie A4).

**Pour modifier** : ajouter une donnée (ex. nombre de voyageurs prévu réel) = ajouter un paramètre au record, le renseigner dans le dépôt, puis l'utiliser dans une règle.

## A3. `Entities/ResourcePool.cs`

**Rôle** : regrouper toutes les ressources disponibles pour l'allocation et répondre à « quelles indisponibilités a cette ressource ? ».

| Instruction | Explication |
| --- | --- |
| `private readonly ILookup<(ResourceKind, long), Unavailability> _unavailabilities;` | Index : clé = (type de ressource, identifiant) -> toutes ses indisponibilités. `ILookup` est comme un dictionnaire dont les valeurs sont des listes. |
| `public ResourcePool(IReadOnlyList<Bus> buses, IReadOnlyList<Agent> agents, IEnumerable<Unavailability> unavailabilities)` | Constructeur : reçoit les bus, les agents et les indisponibilités. |
| `_unavailabilities = unavailabilities.ToLookup(u => (u.Kind, u.ResourceId));` | Construit l'index une fois pour toutes (recherche rapide ensuite). |
| `public IReadOnlyList<Bus> Buses { get; }` / `Agents` | Listes en lecture seule. |
| `public IReadOnlyList<Unavailability> UnavailabilityFor(ResourceKind kind, long id) => _unavailabilities[(kind, id)].ToList();` | Renvoie les indisponibilités d'une ressource. Si elle n'en a aucune, `ILookup` renvoie une **liste vide** (pas d'erreur). |

**Pour modifier** : si vous ajoutez un nouveau type de ressource (ex. remorque), ajoutez une valeur à `ResourceKind` et une liste dans `ResourcePool`.

## A4. `Scheduling/ResourceSchedule.cs`

**Rôle** : le **planning d'UNE ressource** (un bus ou un agent) : la liste de ses voyages triés par heure, avec les calculs dont les règles ont besoin (chevauchement, voyage précédent, heures travaillées...). C'est la mémoire de l'algorithme pendant l'allocation.

| Instruction | Explication |
| --- | --- |
| `private readonly List<Trip> _trips = new();` | Liste interne des voyages. `readonly` : on ne peut pas remplacer la liste elle-même, mais on peut ajouter/retirer des éléments. |
| `public IReadOnlyList<Trip> Trips => _trips;` | Expose la liste **en lecture seule** (les autres classes ne peuvent pas la modifier directement). |
| `public void Add(Trip trip) { _trips.Add(trip); _trips.Sort((a, b) => a.Departure.CompareTo(b.Departure)); }` | Ajoute le voyage puis retrie par heure de départ. `CompareTo` renvoie négatif/0/positif : c'est ce qu'attend `Sort`. L'ordre chronologique est nécessaire pour `Previous` et `Next`. |
| `public bool Remove(Trip trip) => _trips.RemoveAll(t => t.Id == trip.Id) > 0;` | Retire le voyage **par identifiant**. `RemoveAll` renvoie le nombre d'éléments retirés ; `> 0` donne vrai si le voyage était présent. Utilisé par l'ALNS (destruction) et par le recalcul exact des scores. |
| `public ResourceSchedule Clone() { var copy = new ResourceSchedule(); copy._trips.AddRange(_trips); return copy; }` | Copie indépendante. Copier la **liste** suffit car chaque `Trip` est un `record` immuable : personne ne peut modifier un voyage partagé. |
| `public bool Overlaps(TimeInterval interval) => _trips.Any(t => t.Interval.Overlaps(interval));` | Vrai si **au moins un** voyage déjà planifié chevauche l'intervalle. Base des règles A05/B03. |
| `public Trip? Previous(DateTime at) => _trips.Where(t => t.Arrival <= at).MaxBy(t => t.Arrival);` | Dernier voyage **terminé** avant l'instant `at` : on filtre ceux qui finissent avant `at`, puis on prend celui qui finit le plus tard. Renvoie `null` s'il n'y en a pas. Sert au repos, à la continuité de station et de ligne. |
| `public Trip? Next(DateTime at) => _trips.Where(t => t.Departure >= at).MinBy(t => t.Departure);` | Premier voyage qui **démarre** après `at`. Sert à vérifier le battement **après** le voyage (utile quand des voyages sont verrouillés). |
| `private IEnumerable<Trip> On(DateOnly day) => _trips.Where(t => t.WorkDate == day);` | Voyages d'un jour donné (méthode d'aide privée). |
| `public TimeSpan WorkedOn(DateOnly day) => On(day).Aggregate(TimeSpan.Zero, (sum, t) => sum + t.Interval.Duration);` | Heures travaillées un jour : `Aggregate` part de zéro et additionne la durée de chaque voyage. |
| `public int TripCountOn(DateOnly day) => On(day).Count();` | Nombre de voyages du jour. |
| `public double KmOn(DateOnly day) => On(day).Sum(t => t.DistanceKm);` | Kilomètres du jour. |
| `public TimeSpan TotalWorked() => ...` | Heures sur **tout** le planning (jours confondus) : sert à mesurer l'équité de charge. |
| `public TimeSpan SpanOn(DateOnly day, TimeInterval candidate)` | **Amplitude** de la journée en incluant le voyage candidat : `start` = plus petit départ, `end` = plus grande arrivée parmi les voyages du jour et le candidat ; renvoie `end - start`. |
| `var start = candidate.Start; var end = candidate.End; foreach (var t in On(day)) { if (t.Departure < start) start = t.Departure; if (t.Arrival > end) end = t.Arrival; }` | Boucle qui élargit l'intervalle jusqu'à couvrir tous les voyages. |

**Pour modifier** : si l'entreprise compte le temps de travail autrement (par exemple en ajoutant 10 minutes de préparation par voyage), changez `WorkedOn` à **un seul endroit** : toutes les règles en profitent.

**Performance** : toutes ces méthodes parcourent la liste (coût proportionnel au nombre de voyages de la ressource, quelques dizaines). Pour des milliers de voyages par ressource, remplacez les `Where(...)` par une recherche binaire.

## A5. `Scheduling/ScheduleBook.cs`

**Rôle** : le **registre de tous les plannings** (un par bus, un par agent). C'est l'état partagé de l'allocation.

**Point clé** : un agent a **un seul** planning, qu'il soit chauffeur ou receveur. Résultat : la règle « un agent ne peut pas être chauffeur et receveur en même temps » découle automatiquement du test de chevauchement.

| Instruction | Explication |
| --- | --- |
| `private readonly Dictionary<long, ResourceSchedule> _buses = new();` (et `_agents`) | Deux tables : identifiant -> planning. Les identifiants de bus et d'agents sont dans des tables séparées (un bus 5 et un agent 5 n'ont rien à voir). |
| `public ResourceSchedule Bus(long id) => _buses.TryGetValue(id, out var s) ? s : _buses[id] = new();` | Cherche le planning ; s'il n'existe pas, le **crée**, le range dans la table et le renvoie (`_buses[id] = new()` est une affectation qui vaut la valeur affectée). Effet de bord sans danger : lire un bus inconnu crée un planning vide. |
| `public ResourceSchedule Agent(long id) => ...` | Même chose pour les agents. |
| `public void Commit(Trip trip, long busId, long driverId, long receiverId)` | **Inscrit** un voyage dans les trois plannings (bus, chauffeur, receveur). |
| `public void Remove(Trip trip, long busId, long driverId, long receiverId)` | Opération inverse : retire le voyage des trois plannings. |
| `public ScheduleBook Clone() { var copy = new ScheduleBook(); foreach (var (id, schedule) in _buses) copy._buses[id] = schedule.Clone(); ... return copy; }` | **Copie profonde** : chaque planning est cloné. Indispensable à l'ALNS : on teste un changement sur une copie sans abîmer le plan courant. Une classe peut lire les champs privés d'une autre **instance** de la même classe (`copy._buses`). |

**Pour modifier** : si un nouveau type de ressource doit avoir un planning (ex. remorque), ajoutez un dictionnaire, une méthode d'accès, et complétez `Commit`, `Remove` et `Clone`.

---

# PARTIE B — Domain : politique et choix des contraintes

## B1. `Allocation/AllocationPolicy.cs`

**Rôle** : **tous les réglages** de l'allocation dans un seul objet : paramètres métier, choix des contraintes, compatibilité type de ligne/catégorie de bus, réglages de la métaheuristique. Il est enregistré en JSON et modifié depuis l'interface.

### Paramètres métier

| Propriété | Défaut | Utilisée par | Explication |
| --- | --- | --- | --- |
| `TimeSpan MaxDailyWork` | 8 h | A08 `DailyWorkRule`, AP04 | Durée de travail maximale d'un agent par jour. |
| `TimeSpan MaxAmplitude` | 12 h | A10 `AmplitudeRule` | Écart maximal entre premier départ et dernière arrivée. |
| `TimeSpan MinAgentRest` | 30 min | A07 | Repos minimal d'un agent entre deux voyages. |
| `TimeSpan MinBusTurnaround` | 15 min | B09 | Temps minimal de remise en service d'un bus. |
| `int MaxTripsPerAgentPerDay` | 6 | A09 | Nombre maximal de voyages par agent et par jour. |
| `double MaxBusKmPerDay` | 500 | B11, BP06 | Kilométrage maximal d'un bus par jour. |
| `TimeSpan RestSaturation` | 4 h | AP03 | Au-delà de ce repos, le critère « repos » vaut son maximum. |

**Toutes ces valeurs sont à valider avec la société de transport** (chapitre 10 du document de conception). Elles ne sont jamais écrites en dur dans les règles.

### Contraintes, catégories, ALNS

| Instruction | Explication |
| --- | --- |
| `public Dictionary<string, ConstraintSetting> Constraints { get; set; } = new();` | Pour chaque **code** de contrainte (« A08 », « B07 »...), le mode choisi (désactivée, dure, souple) et le poids. Vide par défaut : le catalogue applique alors ses valeurs par défaut. |
| `public List<LineTypeCategoryRule> CategoryRules { get; set; } = new();` | Règles « type de ligne -> catégories de bus ». |
| `public AlnsSettings Alns { get; set; } = new();` | Réglages de la métaheuristique. |
| `public LineTypeCategoryRule? FindCategoryRule(long? lineTypeId) => lineTypeId is null ? null : CategoryRules.FirstOrDefault(r => r.LineTypeId == lineTypeId);` | Retrouve la règle d'un type de ligne, ou `null` s'il n'y en a pas (= aucune restriction). |

### `LineTypeCategoryRule`

| Propriété | Explication |
| --- | --- |
| `long LineTypeId` | Type de ligne concerné (`drligne.DECTYLI`). |
| `List<long> AllowedCategoryIds` | Catégories de bus **autorisées** (`drcatve.DECATVH`). **Liste vide = aucune restriction.** |
| `List<long> PreferredCategoryIds` | Catégories **préférées** (bonus de score BP03). Doit être incluse dans les autorisées quand celles-ci ne sont pas vides. |

### `AlnsSettings` (réglages de la métaheuristique)

| Propriété | Défaut | Effet |
| --- | --- | --- |
| `MaxIterations` | 3000 | Nombre maximal d'itérations de la recherche. |
| `MaxSeconds` | 20 | Durée maximale en secondes (arrêt de sécurité). |
| `StagnationLimit` | 1500 | Arrêt si le meilleur plan ne s'améliore pas pendant N itérations. |
| `DestroyFractionMin` / `Max` | 5 % / 15 % | Part des voyages affectés **détruits** à chaque itération. Petit = recherche locale fine ; grand = exploration. |
| `InitialTemperature` | 100 | Température de départ du recuit simulé, **dans l'unité de l'objectif**. Haute = accepte plus de dégradations au début. |
| `CoolingRate` | 0,998 | Multiplicateur de température à chaque itération (strictement entre 0 et 1). Plus proche de 1 = refroidissement lent. |
| `Seed` | 42 | Graine aléatoire : même graine = même résultat. |
| `InsertionNoise` | 0,10 | Bruit ajouté au classement lors des réparations (fraction du score maximal), pour diversifier. |
| `UnassignedPenalty` | 100000 | Coût d'un voyage non affecté. **Doit dominer** tous les autres termes de l'objectif. |
| `FairnessWeight` | 10 | Poids de l'écart-type des heures de travail (équité entre agents). |
| `ReactionFactor` | 0,10 | Vitesse d'adaptation des poids des opérateurs (0 = jamais, 1 = uniquement la dernière récompense). |

**Pour modifier** : pour ajouter un paramètre, ajoutez la propriété avec sa valeur par défaut, une ligne dans `PolicyValidator` (contrôle), un champ dans `Parameters.razor`, puis utilisez-le dans la règle. Le fichier JSON existant reste lisible : une propriété absente prend sa valeur par défaut.

## B2. `Constraints/ConstraintModels.cs`

**Rôle** : les petits types qui décrivent « une contrainte et le choix de l'utilisateur à son sujet ».

| Instruction | Explication |
| --- | --- |
| `public enum ConstraintMode { Disabled, Hard, Soft }` | Les trois traitements possibles : ignorée, **dure** (élimine) ou **souple** (ajoute des points). |
| `public enum ConstraintTarget { Bus, Agent }` | À quelle ressource la contrainte s'applique (sert à ranger les lignes dans l'interface). |
| `public sealed class ConstraintSetting { public ConstraintMode Mode { get; set; } public double Weight { get; set; } ... }` | Le **choix** de l'utilisateur : mode et poids. Le poids n'a d'effet qu'en mode souple. |
| `public ConstraintSetting() { }` | Constructeur **vide obligatoire** : le lecteur JSON l'utilise pour créer l'objet puis remplir ses propriétés. |
| `public ConstraintSetting(ConstraintMode mode, double weight) { Mode = mode; Weight = weight; }` | Constructeur pratique pour le code. |
| `public sealed record ConstraintDescriptor(string Code, string Name, string Description, ConstraintTarget Target, IReadOnlyList<ConstraintMode> SupportedModes, ConstraintMode DefaultMode, double DefaultWeight, bool IsLocked);` | La **fiche** d'une contrainte, affichée dans l'interface : `Code` (A08), `Name` (nom court), `Description` (explication), `Target`, `SupportedModes` (modes proposés : une règle d'intervalle minimal n'est pas proposée en souple), `DefaultMode`, `DefaultWeight`, `IsLocked` (verrouillée : impossibilité physique). |

**Pour modifier** : un quatrième mode (par exemple « souple avec pénalité forte ») demande d'ajouter une valeur à `ConstraintMode`, de la gérer dans `ConstraintCatalog.BuildRuleSet` et dans `Constraints.razor`.

---

# PARTIE C — Domain : les règles (dures et souples)

Une **règle** est une petite classe qui répond à **une seule question** sur un candidat (un bus ou un agent) pour un voyage donné. Deux familles :

- **Règle dure** : répond « oui/non ». Si non, le candidat est éliminé.
- **Règle souple** : répond par un nombre entre 0 et 1. Multiplié par le poids, il donne des points de score.

## C1. `Rules/Contracts.cs` — les contrats communs

| Instruction | Explication |
| --- | --- |
| `public sealed record RuleViolation(string Code, string Message);` | Le résultat d'une règle dure **violée** : le code de la règle (« A08 ») et un message. Une règle respectée renvoie `null` à la place. |
| `public sealed record ScoreDetail(string Code, double Weight, double Value) { public double Points => Weight * Value; }` | Contribution d'**un critère souple** : poids, valeur normalisée (0 à 1) et points = poids × valeur. La liste de ces détails sert à la **justification** affichée. |
| `public sealed record RuleContext<T>(T Resource, Trip Trip, ResourceSchedule Schedule, IReadOnlyList<Unavailability> Unavailabilities, AllocationPolicy Policy);` | **Tout ce dont une règle a besoin** : la ressource évaluée (`T` = `Bus` ou `Agent`), le voyage, **le planning de cette ressource**, ses indisponibilités et la politique. Une règle ne reçoit rien d'autre : c'est une **fonction pure** (mêmes entrées = même résultat), donc facile à tester. |
| `public interface IHardRule<T> { string Code { get; } RuleViolation? Check(RuleContext<T> context); }` | Contrat d'une règle dure : un code et une méthode `Check` qui renvoie une violation ou `null`. |
| `public interface ISoftRule<T> { string Code { get; } double Weight { get; } double Evaluate(RuleContext<T> context); }` | Contrat d'une règle souple : un code, un poids, et `Evaluate` qui renvoie une valeur entre 0 et 1. |
| `public sealed class RuleSet<T>(IReadOnlyList<IHardRule<T>> hard, IReadOnlyList<ISoftRule<T>> soft)` | Un **jeu de règles** : la liste des règles dures et la liste des règles souples **actives** pour un type de ressource. Construit par `ConstraintCatalog`. |
| `public IReadOnlyList<IHardRule<T>> Hard { get; } = hard;` (et `Soft`) | Les paramètres du constructeur principal sont recopiés dans des propriétés publiques. |
| `public double MaxScore => Soft.Sum(s => s.Weight);` | Score maximal théorique de ce jeu = somme des poids des règles souples actives. |

## C2. `Rules/Hard/GenericHardRules.cs` — règles dures valables pour tout `T`

Ces trois classes sont **génériques** : la même classe sert aux bus et aux agents, seul le **code** change.

### `NoOverlapRule<T>(string code)` — A05 (agents) / B03 (bus)

| Instruction | Explication |
| --- | --- |
| `public string Code => code;` | Le code reçu au constructeur (« A05 » ou « B03 »). |
| `c.Schedule.Overlaps(c.Trip.Interval) ? new(code, "Déjà affecté à un voyage qui chevauche") : null` | Si le planning de la ressource contient déjà un voyage qui chevauche, **violation** ; sinon `null`. C'est la règle fondamentale : une ressource ne peut pas être à deux endroits. |

### `UnavailabilityRule<T>(string code)` — A04 (congés) / B10 (maintenance)

| Instruction | Explication |
| --- | --- |
| `c.Unavailabilities.FirstOrDefault(u => u.Period.Overlaps(c.Trip.Interval)) is { } u ? new(code, $"Indisponible : {u.Reason}") : null` | Cherche **la première** indisponibilité qui chevauche le voyage. Si elle existe (`is { } u`), violation avec la raison (« Congé », « Maintenance ») ; sinon `null`. |

### `MinGapRule<T>(string code, Func<AllocationPolicy, TimeSpan> minGap)` — A07 (repos agent) / B09 (remise en service bus)

| Instruction | Explication |
| --- | --- |
| `Func<AllocationPolicy, TimeSpan> minGap` | La règle reçoit **une fonction** qui lit le seuil dans la politique : `p => p.MinAgentRest` pour les agents, `p => p.MinBusTurnaround` pour les bus. Ainsi une seule classe sert aux deux cas. |
| `var min = minGap(c.Policy);` | Valeur du seuil. |
| `var prev = c.Schedule.Previous(c.Trip.Departure);` | Voyage terminé juste avant le départ. |
| `if (prev is not null && c.Trip.Departure - prev.Arrival < min) return new(code, ...);` | S'il existe et que l'écart entre sa fin et le départ du nouveau voyage est **inférieur au minimum** : violation. |
| `var next = c.Schedule.Next(c.Trip.Arrival);` | Voyage qui démarre juste après l'arrivée. |
| `if (next is not null && next.Departure - c.Trip.Arrival < min) return new(code, ...);` | Même contrôle **après** le voyage. Indispensable quand des voyages sont déjà verrouillés dans le planning. |
| `return null;` | Aucun problème. |

**Un écart exactement égal au minimum est accepté** (`<` strict). Dans la démo : un bus qui finit à 09:00 ne peut pas partir à 09:00 (écart 0 < 15 min), mais peut partir à 09:15.

## C3. `Rules/Hard/BusRules.cs`

| Classe | Code | Condition de violation | Remarque |
| --- | --- | --- | --- |
| `BusActiveRule` | B02 | `!c.Resource.Active` | Un bus inactif n'est **jamais** affecté. Verrouillée. |
| `BusCapacityRule` | B05 | `c.Trip.ExpectedPassengers is { } n && c.Resource.Capacity < n` | N'agit **que si** le nombre de voyageurs prévu est connu (`is { } n`). Aujourd'hui `null` : règle sans effet. |
| `BusDailyKmRule` | B11 | `c.Schedule.KmOn(c.Trip.WorkDate) + c.Trip.DistanceKm > c.Policy.MaxBusKmPerDay` | Km déjà faits ce jour + km de ce voyage > maximum. |

## C4. `Rules/Hard/AgentRules.cs`

| Classe | Code | Condition de violation | Explication |
| --- | --- | --- | --- |
| `RestDayRule` | A03 | `c.Trip.Departure.DayOfWeek == rest \|\| c.Trip.Arrival.DayOfWeek == rest` | `if (c.Resource.RestDay is not { } rest) return null;` : sans jour de repos connu, pas de violation. Test sur le jour de **départ et d'arrivée** (voyage qui passe minuit). |
| `DailyWorkRule` | A08 | `WorkedOn(jour) + durée du voyage > MaxDailyWork` | Les heures déjà travaillées + ce voyage ne doivent pas dépasser 8 h (valeur de la politique). |
| `AmplitudeRule` | A10 | `SpanOn(jour, voyage) > MaxAmplitude` | L'amplitude de la journée avec ce voyage ne doit pas dépasser la limite. |
| `MaxTripsPerDayRule` | A09 | `TripCountOn(jour) >= MaxTripsPerAgentPerDay` | Nombre de voyages **déjà** faits : `>=` car on s'apprête à en ajouter un. |

## C5. `Rules/Hard/ConfigurableHardRules.cs` — règles qui existent aussi en version souple

### `SameValueHardRule<T>(code, message, resourceKey, tripKey)` — centre et délégation en mode **dur**

| Instruction | Explication |
| --- | --- |
| `Func<T, long?> resourceKey` / `Func<Trip, long?> tripKey` | Deux fonctions qui extraient la valeur à comparer : par exemple `b => b.CenterId` (centre du bus) et `t => t.CenterId` (centre de la ligne). La même classe sert pour le centre **et** la délégation, bus **et** agents. |
| `return resourceValue is not null && tripValue is not null && resourceValue != tripValue ? new RuleViolation(code, message) : null;` | Violation **seulement si les deux valeurs sont connues et différentes**. Une donnée absente ne bloque pas (bénéfice du doute). |

### `CategoryCompatibilityRule` — B06 en mode **dur**

| Instruction | Explication |
| --- | --- |
| `var rule = c.Policy.FindCategoryRule(c.Trip.LineTypeId);` | Règle de compatibilité du type de ligne de ce voyage. |
| `if (rule is null \|\| rule.AllowedCategoryIds.Count == 0) return null;` | Aucune règle ou liste vide : **aucune restriction**. |
| `return c.Resource.CategoryId is { } category && rule.AllowedCategoryIds.Contains(category) ? null : new RuleViolation(Code, "Catégorie de bus non autorisée pour ce type de ligne");` | Le bus passe seulement si sa catégorie est dans la liste des autorisées. Un bus **sans catégorie** est refusé dès qu'une restriction existe. |

## C6. `Rules/Soft/SoftRules.cs` — les critères de préférence

**Convention commune : 1 = idéal, 0 = pire cas, 0,5 = donnée inconnue** (ni bonus ni pénalité). Chaque classe est construite avec un poids (`double weight`) qui vient des choix de l'utilisateur.

| Classe | Code | Calcul de la valeur (0 à 1) |
| --- | --- | --- |
| `MatchRule<T>(code, weight, resourceKey, tripKey)` | B07, B08, A11, A12 | Si l'une des valeurs est inconnue : 0,5 ; si égales : 1 ; sinon 0. Sert pour **centre** et **délégation**, bus et agents. |
| `FewTripsRule<T>(code, weight)` | BP07, AP05 | `1.0 / (1 + nombre de voyages du jour)` : 0 voyage -> 1, 1 voyage -> 0,5, 3 voyages -> 0,25. Équilibre naturel de la charge. |
| `CategoryAllowedSoftRule(weight)` | B06 (souple) | 0,5 si aucune règle ; 1 si la catégorie est autorisée ; 0 sinon. |
| `CategoryPreferenceSoftRule(weight)` | BP03 | 0,5 si aucune préférence définie ; 0 si le bus n'a pas de catégorie ; 1 si catégorie **préférée** ; 0,5 si simplement autorisée ; 0 sinon. C'est elle qui **garde les bus confort** pour les lignes longues. |
| `CapacityFitRule(weight)` | BP04 | `Math.Clamp((double)n / capacité, 0, 1)` : plus la demande prévue remplit le bus, mieux c'est ; 0,5 si la demande est inconnue. `Math.Clamp(v, 0, 1)` borne la valeur entre 0 et 1. |
| `StationContinuityRule(weight)` | BP05 | 0,5 si pas de voyage précédent ou stations inconnues ; 1 si la **destination du voyage précédent = origine du nouveau** ; sinon 0 (déplacement à vide). |
| `LowKmRule(weight)` | BP06 | `1 - Math.Clamp(kmDuJour / MaxBusKmPerDay, 0, 1)` : un bus qui a peu roulé est préféré. |
| `NewerBusRule(weight, maxAgeYears = 20)` | BP08 | `1 - Math.Clamp(âge / 20 ans, 0, 1)` ; 0,5 si la date de mise en circulation est inconnue. `(c.Trip.Departure - since).TotalDays / 365.25` convertit en années. |
| `RestTimeRule(weight)` | AP03 | 1 si aucun voyage précédent ; sinon `Math.Clamp((départ - fin précédent) / RestSaturation, 0, 1)` : l'agent le plus reposé gagne (division de deux `TimeSpan` = nombre). |
| `LowWorkedHoursRule(weight)` | AP04 | `1 - Math.Clamp(heuresDuJour / MaxDailyWork, 0, 1)` : l'agent le moins chargé gagne. |
| `SameLineContinuityRule(weight)` | AP06 | 0,5 si pas de voyage précédent ; 1 si même ligne ; sinon 0. |

**Pour ajouter une règle** (exemple : « un chauffeur doit être habilité sur la ligne ») :

1. Créer `Rules/Hard/LineQualificationRule.cs` qui implémente `IHardRule<Agent>` (`Code` => « A14 », `Check` renvoie une violation ou `null`).
2. Si la règle a besoin d'une donnée nouvelle, l'ajouter à l'entité (`Agent`), au dépôt MySQL et éventuellement à une table.
3. L'ajouter dans `ConstraintCatalog` (partie D) : une ligne `HardToggle<Agent>(...)`.
4. Écrire un test dans `ConstraintTests`.

L'interface la propose **automatiquement** (elle lit le catalogue).

---

# PARTIE D — Domain : le catalogue de contraintes

## D1. `Constraints/ConstraintCatalog.cs`

**Rôle** : **la liste de TOUTES les contraintes** du système, avec pour chacune : sa fiche (nom, description, modes possibles, valeurs par défaut) et la façon de fabriquer la règle correspondante. C'est **le** fichier à modifier pour ajouter, retirer ou reclasser une contrainte. L'interface Blazor et le moteur lisent tous deux ce catalogue.

### Le type interne `ConstraintDefinition<T>`

| Instruction | Explication |
| --- | --- |
| `internal sealed record ConstraintDefinition<T>(ConstraintDescriptor Descriptor, Func<IHardRule<T>>? HardFactory, Func<double, ISoftRule<T>>? SoftFactory);` | Une définition = une **fiche** + deux **fabriques** facultatives. `HardFactory` est une fonction sans argument qui crée la règle dure. `SoftFactory` est une fonction qui reçoit **le poids** et crée la règle souple. Une règle uniquement souple n'a pas de `HardFactory` (`null`), et inversement. `internal` : invisible hors du projet Domain. |

### Les listes statiques

| Instruction | Explication |
| --- | --- |
| `private static readonly IReadOnlyList<ConstraintDefinition<Bus>> BusDefinitions = CreateBusDefinitions();` | Construite **une seule fois** au premier usage de la classe. Idem pour `AgentDefinitions`. **L'ordre dans la liste est l'ordre d'évaluation des règles dures** : les plus économiques en premier (B02 « bus actif » avant B03 « chevauchement »...). Cet ordre influence seulement les statistiques de rejet (seule la **première** cause est comptée), pas le résultat. |
| `private static readonly IReadOnlyList<ConstraintDescriptor> AllDescriptorList = BusDefinitions.Select(d => d.Descriptor).Concat(AgentDefinitions.Select(d => d.Descriptor)).ToList();` | Toutes les fiches (bus puis agents), pour l'interface. |
| `public static IReadOnlyList<ConstraintDescriptor> AllDescriptors => AllDescriptorList;` | Accès public aux fiches. |
| `public static ConstraintDescriptor? FindDescriptor(string code) => AllDescriptorList.FirstOrDefault(d => d.Code == code);` | Retrouve une fiche par son code (ou `null`). |
| `public static string DescribeCode(string code) => FindDescriptor(code) is { } d ? $"{d.Code} — {d.Name}" : code;` | Transforme « A08 » en « A08 — Heures maximales par jour » pour les rapports et info-bulles. |

### Résolution des choix de l'utilisateur

| Instruction | Explication |
| --- | --- |
| `ResolveSetting(AllocationPolicy policy, ConstraintDescriptor descriptor)` | Détermine le choix **effectif** pour une contrainte. |
| `if (descriptor.IsLocked) return new ConstraintSetting(descriptor.DefaultMode, descriptor.DefaultWeight);` | Une contrainte verrouillée ignore toute configuration : on **ne peut pas désactiver** le chevauchement, même en modifiant le JSON à la main. |
| `if (policy.Constraints.TryGetValue(descriptor.Code, out var configured) && descriptor.SupportedModes.Contains(configured.Mode)) return new ConstraintSetting(configured.Mode, Math.Max(0, configured.Weight));` | Si l'utilisateur a configuré cette contrainte **avec un mode autorisé**, on l'utilise (poids négatif ramené à 0). |
| `return new ConstraintSetting(descriptor.DefaultMode, descriptor.DefaultWeight);` | Sinon : valeurs par défaut. Un mode non supporté (ex. « souple » pour l'écart minimal) est donc **corrigé silencieusement**. |
| `NormalizeSettings(AllocationPolicy policy)` | Recrée `policy.Constraints` avec **une entrée valide pour chaque contrainte connue**. Appelée au chargement du JSON : ajouter une contrainte au catalogue ne casse pas un ancien fichier de réglages. |

### Construction des jeux de règles

| Instruction | Explication |
| --- | --- |
| `public static RuleSet<Bus> BuildBusRuleSet(AllocationPolicy policy) => BuildRuleSet(BusDefinitions, policy);` | Jeu de règles **des bus** selon les choix. `BuildAgentRuleSet` fait de même pour les agents. |
| `private static RuleSet<T> BuildRuleSet<T>(IEnumerable<ConstraintDefinition<T>> definitions, AllocationPolicy policy)` | Méthode générique commune. |
| `var hard = new List<IHardRule<T>>(); var soft = new List<ISoftRule<T>>();` | Les deux listes à remplir. |
| `foreach (var definition in definitions) { var setting = ResolveSetting(policy, definition.Descriptor); ... }` | Pour chaque contrainte : on lit le choix effectif. |
| `case ConstraintMode.Hard when definition.HardFactory is not null: hard.Add(definition.HardFactory()); break;` | Mode **dur** et fabrique dure disponible : on **crée la règle** (`HardFactory()` appelle la fonction) et on l'ajoute aux règles dures. |
| `case ConstraintMode.Soft when definition.SoftFactory is not null && setting.Weight > 0: soft.Add(definition.SoftFactory(setting.Weight)); break;` | Mode **souple**, fabrique disponible et **poids > 0** : on crée la règle avec son poids. Un poids nul n'aurait aucun effet, on l'ignore. |
| (aucun cas pour `Disabled`) | **Désactivée = aucune règle ajoutée.** |
| `return new RuleSet<T>(hard, soft);` | Jeu de règles final. |

### Les quatre fabriques de définitions (profils de contrainte)

| Méthode | Modes proposés | Mode par défaut | Utilisation |
| --- | --- | --- | --- |
| `LockedHard<T>(code, name, description, target, hard)` | Dure seulement | Dure | Impossibilités physiques : B02, B03, A05. `IsLocked = true`. |
| `HardToggle<T>(...)` | Désactivée / Dure | Dure | Règles qu'on peut désactiver : B05, B09, B10, B11, A03, A04, A07, A08, A09, A10. |
| `HardOrSoft<T>(..., defaultMode, defaultWeight, hard, soft)` | Désactivée / Dure / Souple | au choix | Règles à deux versions : B06 (catégorie), B07/A11 (centre), B08/A12 (délégation). |
| `SoftToggle<T>(..., defaultWeight, soft)` | Désactivée / Souple | Souple | Critères de préférence : BP03 à BP08, AP03 à AP06. |

Exemple lu en entier : `HardToggle<Agent>("A08", "Heures maximales par jour", "Limite quotidienne...", ConstraintTarget.Agent, () => new DailyWorkRule())` signifie : « contrainte A08, pour les agents, désactivable ou dure (dure par défaut) ; pour la fabriquer : `new DailyWorkRule()` ».

Exemple à deux versions : `HardOrSoft<Bus>("B07", "Même centre que la ligne", ..., ConstraintMode.Soft, 40, () => new SameValueHardRule<Bus>("B07", "Centre du bus différent...", b => b.CenterId, t => t.CenterId), w => new MatchRule<Bus>("B07", w, b => b.CenterId, t => t.CenterId))` signifie : « souple par défaut avec poids 40 ; si l'utilisateur choisit **dure**, on utilise `SameValueHardRule` ; s'il choisit **souple**, on utilise `MatchRule` avec le poids `w` ».

### Table complète des contraintes

| Code | Cible | Profil | Défaut | Règle |
| --- | --- | --- | --- | --- |
| B02 | Bus | Verrouillée | Dure | `BusActiveRule` |
| B03 | Bus | Verrouillée | Dure | `NoOverlapRule<Bus>` |
| B10 | Bus | Désactivable | Dure | `UnavailabilityRule<Bus>` |
| B09 | Bus | Désactivable | Dure | `MinGapRule<Bus>` (seuil `MinBusTurnaround`) |
| B05 | Bus | Désactivable | Dure | `BusCapacityRule` |
| B11 | Bus | Désactivable | Dure | `BusDailyKmRule` |
| B06 | Bus | Dure/Souple | Dure (poids 25) | `CategoryCompatibilityRule` / `CategoryAllowedSoftRule` |
| B07 | Bus | Dure/Souple | Souple 40 | `SameValueHardRule` / `MatchRule` (centre) |
| B08 | Bus | Dure/Souple | Souple 25 | idem (délégation) |
| BP03 | Bus | Souple | 25 | `CategoryPreferenceSoftRule` |
| BP04 | Bus | Souple | 20 | `CapacityFitRule` |
| BP05 | Bus | Souple | 20 | `StationContinuityRule` |
| BP06 | Bus | Souple | 15 | `LowKmRule` |
| BP07 | Bus | Souple | 15 | `FewTripsRule<Bus>` |
| BP08 | Bus | Souple | 10 | `NewerBusRule` |
| A05 | Agent | Verrouillée | Dure | `NoOverlapRule<Agent>` |
| A03 | Agent | Désactivable | Dure | `RestDayRule` |
| A04 | Agent | Désactivable | Dure | `UnavailabilityRule<Agent>` |
| A07 | Agent | Désactivable | Dure | `MinGapRule<Agent>` (seuil `MinAgentRest`) |
| A08 | Agent | Désactivable | Dure | `DailyWorkRule` |
| A09 | Agent | Désactivable | Dure | `MaxTripsPerDayRule` |
| A10 | Agent | Désactivable | Dure | `AmplitudeRule` |
| A11 | Agent | Dure/Souple | Souple 40 | centre |
| A12 | Agent | Dure/Souple | Souple 25 | délégation |
| AP03 | Agent | Souple | 20 | `RestTimeRule` |
| AP04 | Agent | Souple | 20 | `LowWorkedHoursRule` |
| AP05 | Agent | Souple | 15 | `FewTripsRule<Agent>` |
| AP06 | Agent | Souple | 10 | `SameLineContinuityRule` |

**Score maximal par défaut d'un voyage** : bus (B07 40 + B08 25 + BP03 25 + BP04 20 + BP05 20 + BP06 15 + BP07 15 + BP08 10 = 170) + 2 × agent (A11 40 + A12 25 + AP03 20 + AP04 20 + AP05 15 + AP06 10 = 130) = **430**. (B06 est dure par défaut, donc sans points.)

**Pour modifier**

- **Changer un défaut** : modifier le mode ou le poids dans la ligne du catalogue (n'affecte pas un fichier JSON déjà enregistré).
- **Passer une règle de « désactivable » à « dure ou souple »** : remplacer `HardToggle` par `HardOrSoft` et fournir la version souple.
- **Retirer une contrainte** : supprimer sa ligne ; `NormalizeSettings` nettoiera l'ancien JSON.

---

# PARTIE E — Domain : le moteur d'allocation

## E1. `Allocation/CandidateSelector.cs` — filtrer puis noter

**Rôle** : le moteur commun qui applique un jeu de règles à une liste de candidats. Il sert aux bus **et** aux agents.

### Les types

| Instruction | Explication |
| --- | --- |
| `public sealed record Candidate<T>(long Id, T Resource, ResourceSchedule Schedule, IReadOnlyList<Unavailability> Unavailabilities);` | Un **candidat à évaluer** : sa ressource, son planning actuel et ses indisponibilités. |
| `public sealed record ScoredCandidate<T>(long Id, T Resource, double Score, IReadOnlyList<ScoreDetail> Details, double RankingKey);` | Un candidat **retenu** : `Score` = vrai score ; `Details` = points critère par critère ; `RankingKey` = score + bruit éventuel, **utilisée uniquement pour trier**. |
| `public sealed record SelectionResult<T>(IReadOnlyList<ScoredCandidate<T>> Candidates, IReadOnlyDictionary<string, int> RejectionsByCode)` | Résultat d'une sélection : candidats classés + **compteur de rejets par code de règle** (ex. « A08 » -> 3). |
| `public ScoredCandidate<T>? Best => Candidates.Count > 0 ? Candidates[0] : null;` | Le meilleur (premier de la liste triée), ou `null` si aucun candidat n'est valide. |

### Les méthodes

| Instruction | Explication |
| --- | --- |
| `FindFirstViolation<T>(RuleContext<T> context, RuleSet<T> rules)` | Parcourt les règles dures dans l'ordre ; **s'arrête à la première violation** et la renvoie (`null` si le candidat est éligible). |
| `ComputeScoreDetails<T>(...) => rules.Soft.Select(r => new ScoreDetail(r.Code, r.Weight, Math.Clamp(r.Evaluate(context), 0, 1))).ToList();` | Pour chaque règle souple, évalue et **borne la valeur entre 0 et 1** (protège contre une règle mal écrite). |
| `CountFeasible<T>(...) => candidates.Count(c => FindFirstViolation(new RuleContext<T>(...), rules) is null);` | **Compte** les candidats éligibles (sans calculer de score). Sert à mesurer la **difficulté** d'un voyage. |
| `Select<T>(candidates, trip, rules, policy, Random? noiseSource = null, double noiseFraction = 0)` | La sélection complète. Les deux derniers paramètres (facultatifs) ne servent qu'à l'ALNS. |
| `var noiseAmplitude = noiseFraction * rules.MaxScore;` | Amplitude du bruit = fraction du score maximal (0 si pas de bruit). |
| `foreach (var candidate in candidates) { var context = new RuleContext<T>(...); ... }` | Pour chaque candidat : construit son contexte. |
| `var violation = FindFirstViolation(context, rules); if (violation is not null) { rejections[violation.Code] = rejections.GetValueOrDefault(violation.Code) + 1; continue; }` | **Étape 1, règles dures** : si violation, on compte le rejet pour ce code et on passe au candidat suivant (`continue`). `GetValueOrDefault` renvoie 0 si le code n'a pas encore été compté. |
| `var details = ComputeScoreDetails(context, rules); var score = details.Sum(d => d.Points);` | **Étape 2, règles souples** : score = somme des points. |
| `var noise = noiseSource is null ? 0 : (noiseSource.NextDouble() * 2 - 1) * noiseAmplitude;` | `NextDouble()` donne un nombre entre 0 et 1 ; `* 2 - 1` le ramène entre -1 et +1 ; multiplié par l'amplitude. Sans source aléatoire : 0. |
| `accepted.Add(new ScoredCandidate<T>(candidate.Id, candidate.Resource, score, details, score + noise));` | Retient le candidat. |
| `var sorted = accepted.OrderByDescending(x => x.RankingKey).ThenBy(x => x.Id).ToList();` | Tri : meilleure clé d'abord ; à égalité, **identifiant croissant** → résultat **déterministe**. |

**Pourquoi deux étapes ?** Une règle dure ne peut jamais être « rachetée » par un bon score. Un bus inactif avec un score potentiel de 400 reste éliminé.

## E2. `Allocation/JustificationFormatter.cs`

| Instruction | Explication |
| --- | --- |
| `public static string Explain(string label, long resourceId, double score, IEnumerable<ScoreDetail> details)` | Fabrique le texte de justification d'un choix. |
| `var parts = details.Where(d => d.Points > 0).Select(d => $"{d.Code}={d.Points:0.#}");` | Garde les critères qui ont rapporté des points ; `{d.Points:0.#}` affiche un chiffre après la virgule seulement si nécessaire. |
| `return $"{label} #{resourceId} : {score:0.#} pts [{string.Join(", ", parts)}]";` | Exemple : `Bus #3 : 87,5 pts [B07=40, BP04=10, BP06=15]`. |

## E3. `Allocation/TripAllocator.cs` — la décision pour UN voyage

### Les types de résultat

| Type | Contenu |
| --- | --- |
| `TripAssignment(Trip, Bus, Agent Driver, Agent Receiver, double Score, double MaxScore, string Justification)` | Une affectation complète : le voyage, le bus, le chauffeur, le receveur, le score obtenu, le score maximal possible et le texte de justification. |
| `UnassignedTrip(Trip, IReadOnlyList<string> MissingRoles, IReadOnlyDictionary<string,int> RejectionsByCode)` | Un voyage **non affecté** : rôles manquants (« BUS », « DRIVER », « RECEIVER ») et causes de rejet. |
| `TripAllocationOutcome(TripAssignment? Assignment, UnassignedTrip? Failure)` | Résultat d'une tentative : **soit** une affectation, **soit** un échec expliqué. |
| `AllocationResult(Assignments, Unassigned)` | Résultat d'une allocation gloutonne simple. |

### Le constructeur et les propriétés

| Instruction | Explication |
| --- | --- |
| `public TripAllocator(AllocationPolicy policy) { Policy = policy; BusRules = ConstraintCatalog.BuildBusRuleSet(policy); AgentRules = ConstraintCatalog.BuildAgentRuleSet(policy); }` | À la création, l'allocateur **construit ses jeux de règles** selon les choix de la politique. Changer un choix = créer un nouvel allocateur. |
| `public double MaxScorePerTrip => BusRules.MaxScore + 2 * AgentRules.MaxScore;` | Score maximal d'un voyage (bus + chauffeur + receveur). |

### `TryAssign` — la méthode centrale

| Instruction | Explication |
| --- | --- |
| `public TripAllocationOutcome TryAssign(Trip trip, ResourcePool pool, ScheduleBook book, bool explain = false, Random? noiseSource = null)` | Cherche le meilleur triplet pour **un** voyage. Elle **ne modifie jamais** `book` : c'est à l'appelant d'enregistrer. `explain` : construire ou non le texte de justification (inutile et coûteux pendant l'ALNS). |
| `var noise = noiseSource is null ? 0 : Policy.Alns.InsertionNoise;` | Bruit actif seulement si une source aléatoire est fournie. |
| `var buses = CandidateSelector.Select(BuildBusCandidates(pool, book), trip, BusRules, Policy, noiseSource, noise);` | Sélection parmi **tous les bus**. |
| `var drivers = ... AgentRole.Driver ...` / `var receivers = ... AgentRole.Receiver ...` | Même chose parmi les agents de rôle chauffeur, puis receveur (même jeu de règles `AgentRules`). |
| `var bus = buses.Best; var driver = drivers.Best;` | Les meilleurs, ou `null`. |
| `var receiver = receivers.Candidates.FirstOrDefault(c => c.Id != driver?.Id);` | Le meilleur receveur **différent du chauffeur** (`driver?.Id` : si `driver` est `null`, la valeur est `null`). Cette ligne est une sécurité : normalement un agent n'a qu'un rôle. |
| `if (bus is null \|\| driver is null \|\| receiver is null) { ... }` | Si **un seul** rôle manque, **tout le voyage reste non affecté** (« tout ou rien » : pas d'affectation partielle). |
| `var missing = new List<string>(); if (bus is null) missing.Add("BUS"); ...` | Liste des rôles manquants. |
| `foreach (var source in new[] { buses.RejectionsByCode, drivers.RejectionsByCode, receivers.RejectionsByCode }) foreach (var (code, count) in source) rejections[code] = rejections.GetValueOrDefault(code) + count;` | Additionne les causes de rejet des trois sélections pour expliquer l'échec. |
| `return new TripAllocationOutcome(null, new UnassignedTrip(trip, missing, rejections));` | Échec expliqué. |
| `var justification = explain ? string.Join(" \| ", Explain("Bus"...), Explain("Chauffeur"...), Explain("Receveur"...)) : string.Empty;` | Texte de justification si demandé. |
| `var assignment = new TripAssignment(trip, bus.Resource, driver.Resource, receiver.Resource, bus.Score + driver.Score + receiver.Score, MaxScorePerTrip, justification);` | **Score du voyage = somme des trois scores.** |

### Les autres méthodes

| Méthode | Explication |
| --- | --- |
| `CountFeasibleOptions(trip, pool, book)` | **Difficulté** d'un voyage = plus petit nombre de candidats éligibles parmi les trois rôles (`Math.Min(buses, Math.Min(drivers, receivers))`). Une valeur faible = voyage très contraint, à traiter tôt. |
| `Allocate(trips, pool, book)` | Glouton chronologique simple : trie les voyages par heure (`OrderBy(t => t.Departure).ThenBy(t => t.Id)`), appelle `TryAssign`, et **enregistre** chaque réussite avec `book.Commit(...)` pour que le voyage suivant voie ce choix. Utilisée par les tests. |
| `BuildBusCandidates(pool, book)` | Transforme chaque bus du parc en `Candidate` (avec son planning `book.Bus(b.Id)` et ses indisponibilités). |
| `BuildAgentCandidates(pool, book, role)` | Même chose pour les agents, **filtrés par rôle** (`Where(a => a.Role == role)`) : c'est le contrôle de qualification (A01/A02). |

**Pour modifier**

- **Accepter une affectation partielle** (bus + chauffeur sans receveur) : changer la condition `if (bus is null || driver is null || receiver is null)`.
- **Autoriser la même personne chauffeur et receveur** (déconseillé) : retirer le filtre `c.Id != driver?.Id` et la règle verrouillée A05.
- **Ajouter un quatrième rôle** (contrôleur) : ajouter une sélection, un champ dans `TripAssignment`, `Commit` dans `ScheduleBook`.

## E4. `Allocation/AllocationState.cs` — le plan en cours de construction

**Rôle** : regrouper **les affectations réalisées** et **les plannings** des ressources. C'est l'objet que l'ALNS copie, détruit en partie, puis répare.

| Instruction | Explication |
| --- | --- |
| `private readonly Dictionary<long, TripAssignment> _assignments;` | Affectations indexées par **identifiant de voyage**. |
| `public AllocationState(IReadOnlyList<Trip> tripsToAllocate, ScheduleBook book) : this(tripsToAllocate, book, new Dictionary<long, TripAssignment>()) { }` | Constructeur public : état **vide**. `: this(...)` appelle le constructeur privé en lui passant un dictionnaire vide. `book` contient déjà les voyages verrouillés. |
| `private AllocationState(IReadOnlyList<Trip> tripsToAllocate, ScheduleBook book, Dictionary<long, TripAssignment> assignments)` | Constructeur privé utilisé aussi par `Clone`. |
| `public IReadOnlyList<Trip> AllTrips { get; }` | Tous les voyages à planifier (affectés ou non). |
| `public ScheduleBook Book { get; }` | Les plannings. |
| `public IReadOnlyDictionary<long, TripAssignment> Assignments => _assignments;` | Vue en lecture seule des affectations. |
| `public IEnumerable<Trip> UnassignedTrips => AllTrips.Where(t => !_assignments.ContainsKey(t.Id));` | Voyages **sans affectation** : tous ceux qui ne figurent pas dans le dictionnaire. |
| `public int UnassignedCount => AllTrips.Count - _assignments.Count;` | Leur nombre (suppose que les identifiants de voyage sont **uniques**). |
| `public void Assign(TripAssignment assignment)` | Enregistre une affectation. |
| `if (_assignments.ContainsKey(assignment.Trip.Id)) throw new InvalidOperationException(...);` | **Garde-fou** : affecter deux fois le même voyage est une erreur de programmation. |
| `Book.Commit(assignment.Trip, assignment.Bus.Id, assignment.Driver.Id, assignment.Receiver.Id); _assignments[assignment.Trip.Id] = assignment;` | Met à jour les **trois plannings** puis le dictionnaire. |
| `public bool Unassign(long tripId) { if (!_assignments.Remove(tripId, out var assignment)) return false; Book.Remove(...); return true; }` | Annule une affectation : `Remove(tripId, out var assignment)` retire du dictionnaire **et** récupère l'affectation retirée, dont on a besoin pour libérer les trois plannings. |
| `public AllocationState Clone() => new(AllTrips, Book.Clone(), new Dictionary<long, TripAssignment>(_assignments));` | **Copie profonde** : plannings clonés, dictionnaire copié (les affectations sont des records immuables, on les partage). |

## E5. `Allocation/ObjectiveFunction.cs` — mesurer la qualité d'un plan

**Rôle** : donner **un nombre** à un plan, pour que la métaheuristique sache s'il est meilleur ou pire. **Plus petit = meilleur** (minimisation).

```
objectif = pénalité × (voyages non affectés)
         + Σ (score maximal − score obtenu)          ← « déficit » de score souple
         + poids × (écart-type des heures des chauffeurs + celui des receveurs)
```

| Instruction | Explication |
| --- | --- |
| `_driverIds = pool.Agents.Where(a => a.Role == AgentRole.Driver).Select(a => a.Id).ToList();` | Identifiants des chauffeurs (et de même pour les receveurs), calculés **une fois** dans le constructeur. |
| `foreach (var assignment in state.Assignments.Values) scoreDeficit += assignment.MaxScore - assignment.Score;` | Pour chaque affectation, l'écart entre le score maximal et le score obtenu. Un plan parfait a un déficit de 0. |
| `var fairness = StandardDeviationOfHours(state.Book, _driverIds) + StandardDeviationOfHours(state.Book, _receiverIds);` | **Équité** : plus les heures sont inégales entre agents, plus c'est élevé. |
| `return _policy.Alns.UnassignedPenalty * state.UnassignedCount + scoreDeficit + _policy.Alns.FairnessWeight * fairness;` | La somme pondérée. |
| `StandardDeviationOfHours` | `hours` = heures de chaque agent (`book.Agent(id).TotalWorked().TotalHours`) ; `mean` = moyenne ; `Math.Sqrt(hours.Average(h => (h - mean) * (h - mean)))` = **écart-type** (racine de la moyenne des carrés des écarts à la moyenne). Zéro si tous travaillent autant. |

**Pourquoi une pénalité de 100 000 ?** Le déficit de score d'un voyage vaut au plus \~430. Un voyage non affecté coûte 100 000 : **aucune** amélioration de score ne peut compenser un voyage perdu. C'est la hiérarchie « d'abord tout planifier, ensuite la qualité, ensuite l'équité ».

**Approximation volontaire** : pendant la recherche, `assignment.Score` est le score calculé **à l'insertion**. Quand un autre voyage de la même ressource change, ce score pourrait légèrement différer. On accepte cette approximation pour la vitesse ; les **scores exacts** sont recalculés à la fin (E6).

**Pour modifier** : ajoutez un terme (par exemple un coût par bus utilisé) dans `Evaluate`, avec son poids dans `AlnsSettings`.

## E6. `Allocation/PlanEvaluator.cs` — scores exacts du plan final

| Instruction | Explication |
| --- | --- |
| `var book = state.Book.Clone();` | On travaille sur une **copie** : l'état d'origine reste intact. |
| `foreach (var a in state.Assignments.Values.OrderBy(x => x.Trip.Departure).ThenBy(x => x.Trip.Id))` | Pour chaque affectation, dans l'ordre chronologique. |
| `var bus = ScoreResource("Bus", a.Bus, a.Bus.Id, ResourceKind.Bus, book.Bus(a.Bus.Id), a.Trip, allocator.BusRules, allocator.Policy, pool);` | Recalcule le score du bus (de même pour le chauffeur et le receveur). |
| `result.Add(a with { Score = bus.Score + driver.Score + receiver.Score, MaxScore = ..., Justification = string.Join(" \| ", bus.Text, driver.Text, receiver.Text) });` | Copie de l'affectation avec **score exact et justification** (`with` : copie modifiée d'un record). |
| `ScoreResource<T>(...)` : `schedule.Remove(trip);` | On **retire le voyage** du planning de la ressource avant de la noter : sinon le voyage se compterait lui-même (ses propres heures, son propre « voyage précédent »). |
| `try { ... } finally { schedule.Add(trip); }` | Quoi qu'il arrive, le voyage est **remis** dans le planning. |
| `var details = CandidateSelector.ComputeScoreDetails(context, rules); var score = details.Sum(d => d.Points);` | Même calcul qu'au moment du choix, dans le même contexte. |

## E7. `Allocation/PlanValidator.cs` — le contrôle indépendant

**Rôle** : **rejouer toutes les règles dures** sur le plan final, avec un code distinct de celui qui a construit le plan. Un plan produit par le moteur doit toujours être valide : toute violation signale un **bug**.

| Instruction | Explication |
| --- | --- |
| `public sealed record PlanViolation(long TripId, string Resource, string Code, string Message);` | Une violation détectée. |
| `if (a.Driver.Role != AgentRole.Driver) violations.Add(new PlanViolation(a.Trip.Id, ..., "A02", "Qualification incorrecte (chauffeur)"));` | Vérifie la **qualification** du chauffeur (et du receveur juste après). |
| `if (a.Driver.Id == a.Receiver.Id) violations.Add(... "A06" ...);` | Même personne aux deux postes. |
| `CheckResource(...)` | Pour le bus, le chauffeur et le receveur : **retire** le voyage du planning, évalue **toutes** les règles dures (sans s'arrêter à la première), remet le voyage (`try/finally`). |
| `foreach (var rule in rules.Hard) { var violation = rule.Check(context); if (violation is not null) into.Add(...); }` | Contrairement à la sélection, on **collecte toutes** les violations. |

Le handler **refuse d'enregistrer** un plan dont la liste de violations n'est pas vide.

## E8. `Allocation/FeasibilityAnalyzer.cs` — le diagnostic de faisabilité (phase 0)

**Rôle** : vérifier les **conditions nécessaires** pour tout planifier. Si elles ne sont pas remplies, **aucun algorithme** ne pourra tout affecter : mieux vaut le savoir avant de chercher. Ces bornes sont **indicatives**.

### `PeakConcurrent(trips, extraGap)` — pic de voyages simultanés

| Instruction | Explication |
| --- | --- |
| `var events = new List<(DateTime Time, int Delta)>();` | Liste d'événements : un instant et +1 (début) ou -1 (fin). |
| `events.Add((trip.Departure, +1)); events.Add((trip.Arrival + extraGap, -1));` | Chaque voyage ajoute un début et une fin, **prolongée de `extraGap`** (repos ou remise en service : la ressource n'est libre qu'après). |
| `foreach (var e in events.OrderBy(x => x.Time).ThenBy(x => x.Delta)) { current += e.Delta; if (current > peak) peak = current; }` | Parcours chronologique avec compteur : on garde le **maximum** atteint. `ThenBy(Delta)` place les -1 avant les +1 à égalité d'heure : une ressource libérée est réutilisable à l'instant même. |

### `CountCompatibleBuses(pool, policy, lineTypeId)`

| Instruction | Explication |
| --- | --- |
| `var activeBuses = pool.Buses.Where(b => b.Active);` | Seuls les bus actifs comptent. |
| `var categoryIsHard = ... ConstraintCatalog.ResolveSetting(policy, categoryDescriptor).Mode == ConstraintMode.Hard;` | La catégorie ne limite le nombre de bus **que si B06 est en mode dur**. |
| `if (!categoryIsHard \|\| rule is null \|\| rule.AllowedCategoryIds.Count == 0) return activeBuses.Count();` | Sans restriction : tous les bus actifs. |
| `return activeBuses.Count(b => b.CategoryId is { } c && rule.AllowedCategoryIds.Contains(c));` | Sinon : bus actifs dont la catégorie est autorisée pour ce type de ligne. |

### `Analyze(...)` — les lignes du diagnostic

1. **Bus (tous types de ligne)** : pic simultané (avec `MinBusTurnaround`) comparé aux bus actifs.
2. **Bus compatibles par type de ligne** : pour chaque type (`GroupBy(t => t.LineTypeId)`), pic comparé aux bus compatibles. C'est ce qui révèle « il faut 4 bus confort en même temps, il y en a 3 ».
3. **Chauffeurs et receveurs (simultanés)** : pic avec `MinAgentRest` comparé aux agents du rôle **qui ne sont pas en repos ce jour-là** (`a.RestDay != date.DayOfWeek`).
4. **Chauffeurs et receveurs (heures)** : somme des durées des voyages (`Math.Ceiling`) comparée à `agents × MaxDailyWork` (`Math.Floor`). Si 70 h de conduite sont nécessaires et que 8 chauffeurs × 8 h = 64 h sont disponibles : **impossible**, quelle que soit la méthode.

`Line(scope, demand, supply, unit)` fabrique la ligne : `ok = demand <= supply` et le message « Suffisant : ... » ou « INSUFFISANT : ... ».

## E9. `Allocation/PolicyValidator.cs` — cohérence des réglages

Une suite de tests, chacun ajoutant un message à la liste `errors` :

| Test | Pourquoi |
| --- | --- |
| `MaxDailyWork <= TimeSpan.Zero` | Une durée de travail nulle bloquerait tout. |
| `MaxAmplitude < MaxDailyWork` | L'amplitude (écart premier départ -> dernière arrivée) ne peut pas être plus courte que le temps de travail. |
| repos et remise en service négatifs, `MaxTripsPerAgentPerDay < 1`, `MaxBusKmPerDay <= 0`, `RestSaturation <= 0` | Valeurs absurdes. |
| poids < 0, ou mode souple avec poids 0 | Un poids négatif fausserait le score ; un poids nul rend la règle inutile. |
| catégorie préférée non autorisée | Incohérence de la page « Types de ligne ». |
| paramètres ALNS (itérations, durée, fractions, température, refroidissement entre 0 et 1, bruit, pénalité, équité, réaction, stagnation) | Évite une recherche qui ne démarre pas ou ne se refroidit pas. |

Le service de sauvegarde **refuse** d'écrire une politique qui a des erreurs.

## E10. `Allocation/ConstructionStrategies.cs` — construire le premier plan

| Type | Explication |
| --- | --- |
| `ConstructionPhase(long? LineTypeId, string Label, int TripCount, int PeakDemand, int CompatibleBuses, double Tension, int Assigned)` | Bilan d'une phase (un type de ligne) : combien de voyages, demande maximale, bus compatibles, **tension**, combien affectés. |
| `ConstructionResult(AllocationState State, IReadOnlyList<ConstructionPhase> Phases)` | Le plan construit et ses phases. |
| `interface IConstructionStrategy { ConstructionResult Build(trips, pool, lockedBook, allocator, lineTypeLabels); }` | Contrat d'une stratégie : on peut en ajouter d'autres sans toucher au reste. |

### `ChronologicalConstructionStrategy` (glouton de référence)

| Instruction | Explication |
| --- | --- |
| `var state = new AllocationState(trips, lockedBook);` | État de départ (contenant déjà les voyages verrouillés). |
| `foreach (var trip in trips.OrderBy(t => t.Departure).ThenBy(t => t.Id))` | Voyages **par heure de départ**. |
| `var outcome = allocator.TryAssign(trip, pool, state.Book); if (outcome.Assignment is not null) state.Assign(outcome.Assignment);` | Tentative ; en cas de réussite, enregistrement (qui met à jour les plannings : le voyage suivant en tient compte). |

### `PhasedConstructionStrategy` (construction par phases)

| Instruction | Explication |
| --- | --- |
| `trips.GroupBy(t => t.LineTypeId)` | Un **lot** par type de ligne. |
| `var peak = FeasibilityAnalyzer.PeakConcurrent(g, policy.MinBusTurnaround); var compatible = FeasibilityAnalyzer.CountCompatibleBuses(pool, policy, g.Key);` | Demande maximale simultanée du lot et nombre de bus qui peuvent le servir. |
| `Tension: (double)peak / Math.Max(1, compatible)` | **Tension = demande / offre.** Au-dessus de 1, la demande dépasse l'offre. `(double)` force la division décimale ; `Math.Max(1, ...)` évite la division par zéro. |
| `.OrderByDescending(l => l.Tension).ThenByDescending(l => l.Trips.Count).ThenBy(l => l.Key)` | **Lots les plus tendus en premier** ; à égalité, le plus gros lot ; puis par identifiant. |
| `.Select(t => (Trip: t, Difficulty: allocator.CountFeasibleOptions(t, pool, state.Book))).OrderBy(x => x.Difficulty).ThenBy(x => x.Trip.Departure)...` | Dans un lot : **les voyages les plus contraints d'abord** (peu de candidats). |
| `state.Assign(...)` après chaque `TryAssign` réussi | **L'état est partagé entre les phases** : les agents consommés par un lot sont vus par le lot suivant. |
| `var label = lot.Key is { } id ? lineTypeLabels.GetValueOrDefault(id, $"Type {id}") : "Type inconnu";` | Libellé lisible (« INTERURBAIN »). |

**Pourquoi ça aide** : si un bus confort est rare, le type de ligne qui en dépend est traité d'abord ; l'ordre chronologique pourrait donner ce bus à un voyage urbain du matin et laisser un interurbain sans bus (c'est exactement ce que démontre le test `Phased_construction_gives_scarce_comfort_bus_to_the_line_that_needs_it`).

## E11. `Allocation/PlanCompleter.cs` — dernière chance

**Rôle** : après la métaheuristique (dont les réparations sont bruitées), tenter une affectation **déterministe** des voyages restants, du plus contraint au moins contraint. On ne laisse jamais de côté un voyage affectable.

| Instruction | Explication |
| --- | --- |
| `var ordered = state.UnassignedTrips.Select(t => (Trip: t, Difficulty: ...)).OrderBy(...).Select(x => x.Trip).ToList();` | Voyages restants, du plus difficile au plus facile (`ToList()` fige la liste avant qu'on modifie l'état). |
| `var outcome = allocator.TryAssign(trip, pool, state.Book); if (outcome.Assignment is null) continue; state.Assign(outcome.Assignment); added++;` | Tente, enregistre en cas de succès, compte. |
| `return added;` | Nombre de voyages récupérés. |

---

# PARTIE F — Domain : la métaheuristique ALNS

## F0. L'idée en langage simple

Imaginez le planning de la journée comme un puzzle déjà rempli (la construction). **L'ALNS répète** :

1. **Détruire** : on efface au hasard (ou de façon ciblée) 5 à 15 % des voyages affectés ;
2. **Réparer** : on réaffecte les voyages effacés (et ceux qui étaient déjà sans ressource) d'une autre manière, **en respectant toujours les règles dures** ;
3. **Comparer** : si le nouveau plan est meilleur (objectif plus petit), on le garde. S'il est un peu moins bon, on **peut quand même le garder** avec une probabilité qui diminue avec le temps : c'est le **recuit simulé**. Cela évite de rester coincé dans une solution moyenne ;
4. **Apprendre** : les opérateurs (façons de détruire/réparer) qui donnent de bons résultats sont tirés plus souvent : c'est la partie **adaptative**.

**Recuit simulé** : on accepte une dégradation `Δ` avec la probabilité `exp(-Δ / T)`, où `T` est la **température**. `T` commence haute (on explore) et diminue à chaque itération (`T = T × 0,998`) : on devient de plus en plus exigeant.

## F1. `Alns/AlnsModels.cs` — les types

| Type | Explication |
| --- | --- |
| `RepairContext(TripAllocator Allocator, ResourcePool Pool, Random Random)` | Ce que reçoit un opérateur de **réparation** : l'allocateur (qui connaît les règles), les ressources, et la source aléatoire. |
| `ConvergencePoint(int Iteration, double BestObjective)` | Un point de la **courbe de convergence** (meilleur objectif connu à l'itération N), tracée dans l'interface. |
| `OperatorStatistic(string Name, bool IsDestroy, int Uses, double TotalReward, double FinalWeight)` | Bilan d'un opérateur : nombre d'utilisations, récompense cumulée, poids final. Permet de voir **lesquels sont utiles**. |
| `AlnsProgress(int Iteration, double CurrentObjective, double BestObjective, int UnassignedInBest, TimeSpan Elapsed, double Temperature)` | Progression envoyée à l'interface pendant le calcul. |
| `AlnsResult(AllocationState Best, int Iterations, TimeSpan Elapsed, double InitialObjective, double FinalObjective, IReadOnlyList<ConvergencePoint> Convergence, IReadOnlyList<OperatorStatistic> OperatorStatistics)` | Résultat final : le **meilleur plan**, le nombre d'itérations, les objectifs de départ et d'arrivée, la courbe, les statistiques. |
| `interface IDestroyOperator { string Name { get; } void Destroy(AllocationState state, int count, Random random); }` | Contrat d'un opérateur de **destruction** : annule jusqu'à `count` affectations dans `state`. |
| `interface IRepairOperator { string Name { get; } void Repair(AllocationState state, RepairContext context); }` | Contrat d'un opérateur de **réparation** : tente d'affecter tous les voyages sans affectation. |

## F2. `Alns/Operators.cs`

### Opérateurs de destruction

| Classe | Principe | Instructions importantes |
| --- | --- | --- |
| `RandomRemoval` | Suppression **aléatoire** : diversification pure. | `var ids = state.Assignments.Keys.ToList(); Shuffle(ids, random); foreach (var id in ids.Take(count)) state.Unassign(id);` : liste des identifiants affectés, mélangée, puis les `count` premiers sont désaffectés. `ToList()` est nécessaire car on modifie le dictionnaire pendant la boucle. |
| `Shuffle<T>(IList<T> list, Random random)` | Mélange de **Fisher-Yates** | `for (var i = list.Count - 1; i > 0; i--) { var j = random.Next(i + 1); (list[i], list[j]) = (list[j], list[i]); }` : pour chaque position, échange avec une position aléatoire plus petite ou égale. `(a, b) = (b, a)` est un échange sans variable temporaire. |
| `WorstScoreRemoval` | Retire les **pires affectations** (plus grand écart au score maximal). | `.OrderByDescending(a => (a.MaxScore - a.Score) * (0.5 + random.NextDouble()))` : tri par déficit **multiplié par un facteur aléatoire entre 0,5 et 1,5** : on cible les mauvaises affectations sans toujours retirer les mêmes. |
| `TimeWindowRemoval` | **Voisinage temporel** : retire les voyages dont l'heure est la plus proche d'un instant pivot. | `var unassigned = state.UnassignedTrips.ToList(); var pivotTime = unassigned.Count > 0 && random.NextDouble() < 0.7 ? unassigned[random.Next(unassigned.Count)].Departure : state.Assignments.Values.ElementAt(random.Next(state.Assignments.Count)).Trip.Departure;` : avec 70 % de chances (s'il existe des voyages non affectés), le pivot est **un voyage non affecté** : on libère exactement ce qui le bloque (un « échange » implicite) ; sinon un voyage affecté au hasard. `.OrderBy(a => Math.Abs((a.Trip.Departure - pivotTime).TotalMinutes)).Take(count)` : les `count` voyages les plus proches du pivot dans le temps. |
| `ResourceRemoval` | Vide une partie du planning d'**une ressource** (un bus, un chauffeur ou un receveur). | `var seed = ...ElementAt(random.Next(state.Assignments.Count)); var role = random.Next(3);` : on prend une affectation au hasard et un rôle (0 bus, 1 chauffeur, 2 receveur). `.Where(a => role == 0 ? a.Bus.Id == seed.Bus.Id : role == 1 ? a.Driver.Id == seed.Driver.Id : a.Receiver.Id == seed.Receiver.Id)` : toutes les affectations de cette ressource. La boucle `while (removed < count && ... && attempts++ < 10)` répète avec d'autres ressources jusqu'à atteindre `count` (10 essais maximum pour ne jamais boucler indéfiniment). |

### Opérateurs de réparation

`RepairOperatorBase` est un **patron de méthode** : l'algorithme est écrit une fois, les classes filles ne changent que **l'ordre** de réinsertion et l'usage du bruit.

| Instruction | Explication |
| --- | --- |
| `var pending = state.UnassignedTrips.ToList();` | Voyages à (ré)affecter : ceux qui viennent d'être détruits **et** ceux qui n'avaient jamais pu l'être. |
| `foreach (var trip in Order(pending, state, context))` | Ordre propre à chaque opérateur. |
| `var outcome = context.Allocator.TryAssign(trip, context.Pool, state.Book, explain: false, noiseSource: UseNoise ? context.Random : null);` | Même décision que d'habitude (**mêmes règles dures**), avec ou sans bruit. `explain: false` : pas de texte de justification (gain de temps). |
| `if (outcome.Assignment is not null) state.Assign(outcome.Assignment);` | Si possible, on affecte. **Un plan réparé respecte donc toujours les règles dures.** |

| Classe | Ordre | Bruit | Rôle |
| --- | --- | --- | --- |
| `ChronologicalRepair` | Par heure de départ | Non | Réparation gloutonne classique et déterministe. |
| `MostConstrainedFirstRepair` | Du plus contraint (peu de candidats) au moins contraint | Oui | Donne la priorité aux voyages difficiles (`CountFeasibleOptions`). |
| `RandomOrderRepair` | Aléatoire (`RandomRemoval.Shuffle`) | Oui | Diversification. |

**Le bruit** : dans `CandidateSelector.Select`, un petit aléa est ajouté à la **clé de tri** (pas au score). Il fait parfois préférer le 2e ou le 3e meilleur candidat : sans lui, la réparation redonnerait toujours le même plan.

## F3. `Alns/AlnsImprover.cs` — la boucle principale

### Constantes et opérateurs

| Instruction | Explication |
| --- | --- |
| `private const double RewardNewBest = 33; RewardImproved = 9; RewardAccepted = 3;` | **Récompenses** données à un opérateur selon le résultat : nouveau meilleur plan (33), meilleur que le plan courant (9), accepté malgré une dégradation (3). Valeurs classiques de la littérature ALNS. |
| `private const double MinimumOperatorWeight = 0.1;` | Un opérateur ne tombe jamais à 0 : il garde toujours une chance d'être tiré. |
| `private readonly IReadOnlyList<IDestroyOperator> _destroyOperators = new IDestroyOperator[] { new RandomRemoval(), ... };` | Les **4 opérateurs de destruction**. Les 3 de réparation suivent. **Pour ajouter un opérateur : l'ajouter ici.** |

### Méthode `Improve` — préparation

| Instruction | Explication |
| --- | --- |
| `var settings = allocator.Policy.Alns;` | Réglages de la recherche. |
| `var random = new Random(settings.Seed);` | Source aléatoire **reproductible** (même graine = même résultat). |
| `var objective = new ObjectiveFunction(allocator.Policy, pool);` | Calculateur de l'objectif. |
| `var repairContext = new RepairContext(allocator, pool, random);` | Contexte transmis aux réparations. |
| `var destroyWeights = Enumerable.Repeat(1.0, _destroyOperators.Count).ToArray();` | Un poids de 1 pour chaque opérateur (et de même pour la réparation). Tableaux `destroyUses`, `destroyRewards`, `repairUses`, `repairRewards` : compteurs pour les statistiques. |
| `var current = initial.Clone(); var best = initial.Clone();` | **Copies** : le plan initial fourni n'est jamais modifié. `current` = plan courant ; `best` = meilleur connu. |
| `var currentValue = objective.Evaluate(current); var bestValue = currentValue; var initialValue = currentValue;` | Valeurs de l'objectif. |
| `var temperature = settings.InitialTemperature;` | Température de départ. |
| `var convergence = new List<ConvergencePoint> { new(0, bestValue) };` | Courbe de convergence, premier point à l'itération 0. |
| `var convergenceStep = Math.Max(1, settings.MaxIterations / 200); var progressStep = Math.Max(1, settings.MaxIterations / 100);` | On enregistre environ 200 points de courbe et on envoie environ 100 messages de progression (pas un à chaque itération). |
| `var stopwatch = Stopwatch.StartNew();` | Chronomètre pour la limite de durée. |

### La boucle

| Instruction | Explication |
| --- | --- |
| `while (iteration < settings.MaxIterations && stopwatch.Elapsed.TotalSeconds < settings.MaxSeconds && sinceImprovement < settings.StagnationLimit && !cancellationToken.IsCancellationRequested)` | La recherche continue tant qu'**aucune** des quatre limites n'est atteinte : itérations, durée, stagnation, annulation par l'utilisateur. |
| `var destroyIndex = SelectByRoulette(destroyWeights, random);` (et `repairIndex`) | Choix de l'opérateur **selon les poids** (roulette). |
| `var candidate = current.Clone();` | **Copie** du plan courant : on teste le changement sur la copie. |
| `_destroyOperators[destroyIndex].Destroy(candidate, ChooseDestroyCount(candidate, settings, random), random);` | Détruit sur la copie. |
| `_repairOperators[repairIndex].Repair(candidate, repairContext);` | Répare la copie. |
| `var candidateValue = objective.Evaluate(candidate);` | Qualité du nouveau plan. |
| `if (candidateValue < bestValue - 1e-9) { best = candidate.Clone(); bestValue = candidateValue; current = candidate; ... reward = RewardNewBest; sinceImprovement = 0; }` | **Nouveau meilleur plan** : on le mémorise (copie), il devient aussi le plan courant, récompense maximale, on remet le compteur de stagnation à zéro. `- 1e-9` évite de compter comme amélioration une différence due aux arrondis. |
| `else { sinceImprovement++; if (candidateValue < currentValue - 1e-9) { current = candidate; ... reward = RewardImproved; } ... }` | Pas un record : meilleur que le plan courant -> accepté. |
| `else if (random.NextDouble() < Math.Exp(-(candidateValue - currentValue) / Math.Max(temperature, 1e-9))) { current = candidate; ... reward = RewardAccepted; }` | **Critère du recuit simulé** : plan moins bon, mais accepté avec la probabilité `exp(-Δ/T)`. `Math.Max(temperature, 1e-9)` évite une division par zéro. Sinon : refusé, `current` reste inchangé (la copie `candidate` est simplement abandonnée). |
| `UpdateWeight(destroyWeights, destroyIndex, reward, settings.ReactionFactor);` (et réparation) | **Apprentissage** : le poids de l'opérateur utilisé est mis à jour avec la récompense obtenue. |
| `temperature *= settings.CoolingRate;` | **Refroidissement**. |
| `if (iteration % convergenceStep == 0) convergence.Add(...); if (iteration % progressStep == 0) progress?.Report(...);` | `%` (modulo) : tous les N itérations, on enregistre un point et on envoie la progression. `progress?.Report` : `?.` n'appelle que si `progress` n'est pas `null`. |

### Après la boucle et méthodes d'aide

| Instruction | Explication |
| --- | --- |
| `convergence.Add(...); progress?.Report(...);` | Point final et dernière progression. |
| `for (...) statistics.Add(new OperatorStatistic(...));` | Construit les statistiques de tous les opérateurs. |
| `return new AlnsResult(best, iteration, stopwatch.Elapsed, initialValue, bestValue, convergence, statistics);` | **Rend le meilleur plan** (pas le plan courant, qui peut être légèrement moins bon à cause du recuit). En cas d'**annulation**, la boucle sort proprement et rend le meilleur trouvé jusque-là. |
| `ChooseDestroyCount(state, settings, random)` | `fraction = min + random.NextDouble() × (max − min)` ; nombre = `Math.Round(affectés × fraction)` borné par `Math.Clamp(..., Math.Min(2, affectés), affectés)` : **au moins 2** (ou tous s'il y en a moins), au plus tous. |
| `SelectByRoulette(double[] weights, Random random)` | `threshold = random.NextDouble() × somme des poids` ; on cumule les poids jusqu'à dépasser `threshold` : la probabilité de choisir l'opérateur `i` est **proportionnelle à son poids**. |
| `UpdateWeight(weights, index, reward, reaction) => weights[index] = Math.Max(MinimumOperatorWeight, (1 - reaction) * weights[index] + reaction * reward);` | **Moyenne mobile exponentielle** : 90 % de l'ancien poids + 10 % de la récompense (avec `reaction = 0,1`). Plancher à 0,1. |

**Pour modifier**

- **Chercher plus longtemps** : augmenter `MaxIterations` et `MaxSeconds` (page Paramètres).
- **Plus d'exploration** : augmenter `InitialTemperature`, ou `DestroyFractionMax`.
- **Un opérateur de plus** : créer la classe (`IDestroyOperator` ou `IRepairOperator`) et l'ajouter dans les deux tableaux de `AlnsImprover`.
- **Autre critère d'acceptation** (ex. seuil de recuit déterministe) : modifier le bloc `else if (random.NextDouble() < Math.Exp(...))`.
- **Diagnostiquer** : la table « statistiques des opérateurs » de la page Allocation montre lesquels rapportent. Un opérateur à poids final minimal n'aide pas.

---

# PARTIE G — Application : les cas d'usage et les ports

La couche **Application** orchestre. Elle ne connaît ni MySQL ni Blazor : elle parle à l'extérieur uniquement par des **interfaces** (les « ports »).

## G1. `Abstractions/Ports.cs`

| Interface | Méthodes | Qui l'implémente |
| --- | --- | --- |
| `ITripRepository` | `GetToAllocateAsync(date)` : voyages valides **et** auxquels il manque une ressource. `GetAssignedAsync(from, to)` : voyages déjà **complètement** affectés sur une fenêtre (ils bloquent leurs ressources). | `MySqlTripRepository`, `DemoDataStore` |
| `IResourceRepository` | `GetPoolAsync(from, to)` : bus, agents, indisponibilités. | `MySqlResourceRepository`, `DemoDataStore` |
| `IAssignmentRepository` | `SaveProposalsAsync(assignments, user)` : écrit les propositions. | `MySqlAssignmentRepository`, `DemoDataStore` |
| `IReferenceDataRepository` | `GetLineTypesAsync`, `GetBusCategoriesAsync` : listes (id, libellé) pour les écrans de configuration. | `MySqlReferenceDataRepository`, `DemoDataStore` |
| `IAllocationSettingsStore` | `LoadAsync`, `SaveAsync`, `ResetAsync` : persistance de la politique. | `JsonAllocationSettingsStore` |

`public sealed record ReferenceItem(long Id, string Label);` : un couple identifiant/libellé.

`Task<IReadOnlyList<Trip>>` : une opération **asynchrone** qui renvoie une liste de voyages. `CancellationToken ct` : permet d'annuler la lecture.

**Pour brancher vos données réelles**, c'est ici que se trouve le **contrat** à respecter : tant que vos implémentations renvoient ces objets, le reste fonctionne sans changement.

## G2. `UseCases/AllocationSettingsService.cs`

| Instruction | Explication |
| --- | --- |
| `public sealed record SettingsSaveResult(bool Success, IReadOnlyList<string> Errors);` | Résultat d'une sauvegarde : réussie, ou liste d'erreurs de cohérence. |
| `public sealed class AllocationSettingsService(IAllocationSettingsStore store)` | Reçoit le **port** de persistance (injecté). |
| `LoadAsync(ct) => store.LoadAsync(ct);` | Charge la politique courante. |
| `var errors = PolicyValidator.Validate(policy); if (errors.Count > 0) return new SettingsSaveResult(false, errors);` | **Valide avant d'écrire** : une politique incohérente n'est jamais enregistrée. |
| `await store.SaveAsync(policy, ct); return new SettingsSaveResult(true, errors);` | Sauvegarde puis succès. |
| `ResetAsync(ct) => store.ResetAsync(ct);` | Revient aux valeurs par défaut. |

## G3. `UseCases/PreviewDay.cs` — « analyser la journée »

Ne modifie rien. Charge la politique, les voyages à affecter, les voyages verrouillés, les ressources et les libellés ; calcule `FeasibilityAnalyzer.Analyze(...)` ; compte les voyages par type de ligne (`GroupBy` + `Select`) ; renvoie un `DayPreview` (volumes + diagnostic de faisabilité). `policyOverride` permet d'essayer des réglages sans les enregistrer.

## G4. `UseCases/AllocateDay.cs` — le cas d'usage principal

### Les types

| Type | Explication |
| --- | --- |
| `enum AllocationMethod { GreedyChronological, PhasedConstruction, PhasedConstructionWithAlns }` | Les trois méthodes proposées : (1) glouton chronologique (référence), (2) construction par phases, (3) construction par phases + ALNS. |
| `AllocateDayCommand(DateOnly Date, string User, AllocationMethod Method, bool SaveProposals, AllocationPolicy? PolicyOverride = null)` | La **demande** : quel jour, qui, quelle méthode, enregistrer ou simuler, et éventuellement une politique de remplacement. `= null` : paramètre facultatif. |
| `AlnsSummary(...)` | Synthèse de la métaheuristique : itérations, durée, objectifs, courbe, statistiques d'opérateurs. |
| `AllocationReport(...)` | Le **rapport complet** : compteurs, durée, objectifs de construction et final, codes de règles dures/souples appliquées, affectations, échecs, phases, faisabilité, violations, synthèse ALNS, et `ProposalsSaved`. |

### `HandleAsync` — la partie asynchrone

| Instruction | Explication |
| --- | --- |
| `var policy = command.PolicyOverride ?? await settings.LoadAsync(ct);` | `??` : « si `PolicyOverride` est null, alors charger la politique enregistrée ». |
| `var toAllocate = await trips.GetToAllocateAsync(command.Date, ct);` | Voyages à affecter. |
| `var locked = await trips.GetAssignedAsync(command.Date.AddDays(-1), command.Date.AddDays(1), ct);` | Voyages déjà affectés sur **J-1 à J+1** (pour le repos et les voyages qui passent minuit). |
| `var pool = await resources.GetPoolAsync(command.Date.AddDays(-1), command.Date.AddDays(1), ct);` | Ressources et indisponibilités sur la même fenêtre. |
| `var labels = (await reference.GetLineTypesAsync(ct)).ToDictionary(x => x.Id, x => x.Label);` | Libellés des types de ligne, sous forme de dictionnaire id -> libellé. |
| `var report = await Task.Run(() => Compute(...));` | Le calcul lourd s'exécute sur un **fil d'arrière-plan** : l'interface ne gèle pas. |
| `if (command.SaveProposals && report.Assignments.Count > 0 && report.Violations.Count == 0) { await assignments.SaveProposalsAsync(...); report = report with { ProposalsSaved = true }; }` | **Garde-fou** : on n'enregistre que si c'est demandé, s'il y a des affectations **et** si le contrôle indépendant n'a trouvé **aucune violation**. `with` crée une copie du rapport avec `ProposalsSaved = true`. |

### `Compute` — la partie calcul (statique : sans état, donc sûre)

| Instruction | Explication |
| --- | --- |
| `var allocator = new TripAllocator(policy);` | Moteur de décision selon les contraintes choisies. |
| `var book = new ScheduleBook(); foreach (var a in locked) book.Commit(a.Trip, a.BusId, a.DriverId, a.ReceiverId);` | Les voyages déjà affectés **occupent leurs ressources** avant tout calcul. |
| `var feasibility = FeasibilityAnalyzer.Analyze(...)` | Diagnostic de faisabilité. |
| `IConstructionStrategy strategy = command.Method == AllocationMethod.GreedyChronological ? new ChronologicalConstructionStrategy() : new PhasedConstructionStrategy();` | Choix de la stratégie de construction. |
| `var construction = strategy.Build(toAllocate, pool, book, allocator, lineTypeLabels);` | **Phase 1 : construction.** |
| `var constructionObjective = objective.Evaluate(construction.State);` | Qualité du plan de départ (pour comparer avant/après l'ALNS). |
| `if (command.Method == AllocationMethod.PhasedConstructionWithAlns && toAllocate.Count > 0) { var result = new AlnsImprover().Improve(state, pool, allocator, progress, ct); state = result.Best; ... }` | **Phase 2 : ALNS**, seulement pour la 3e méthode et s'il y a des voyages. `state` devient le meilleur plan trouvé. |
| `PlanCompleter.TryCompleteUnassigned(state, pool, allocator);` | **Phase 3 :** dernière chance déterministe pour les voyages restants. |
| `var finalObjective = objective.Evaluate(state);` | Qualité finale. |
| `var finalAssignments = PlanEvaluator.BuildFinalAssignments(state, pool, allocator);` | **Scores exacts** et justifications. |
| `var violations = PlanValidator.Validate(state, pool, allocator);` | **Contrôle indépendant.** |
| `var failures = state.UnassignedTrips.Select(t => allocator.TryAssign(t, pool, state.Book).Failure).OfType<UnassignedTrip>().OrderBy(f => f.Trip.Departure).ToList();` | Pour chaque voyage resté sans ressource, on **refait une tentative** pour obtenir ses **causes de rejet**. `.OfType<UnassignedTrip>()` ignore les `null`. |
| `var hardCodes = allocator.BusRules.Hard.Select(r => r.Code).Concat(allocator.AgentRules.Hard.Select(r => r.Code)).Distinct().OrderBy(c => c).ToList();` | Codes des règles dures **réellement appliquées** (affichées en badges). Même principe pour les souples. |
| `return new AllocationReport(...)` | Assemble le rapport. |

`ApplicationServiceRegistration.AddApplication` déclare les trois cas d'usage à l'injection de dépendances : `AddTransient<T>()` = une nouvelle instance à chaque demande.

**Pour modifier**

- **Ajouter une quatrième méthode** : ajouter une valeur à `AllocationMethod`, une branche dans `Compute`, un libellé dans `Allocation.razor`.
- **Changer la fenêtre J-1..J+1** (par exemple J-2 pour un repos de 24 h) : modifier les trois appels `AddDays`.
- **Journaliser** : ajouter un `ILogger` au constructeur du handler (le conteneur le fournit).

---

# PARTIE H — Infrastructure : les adaptateurs

## H1. `Settings/JsonAllocationSettingsStore.cs`

| Instruction | Explication |
| --- | --- |
| `public sealed class JsonAllocationSettingsStore(string filePath, Func<AllocationPolicy>? defaultFactory = null)` | Reçoit le chemin du fichier et, facultativement, une fonction qui fabrique la politique **par défaut** (la démo y met ses règles de catégories). |
| `private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };` | Fichier **lisible** (indenté) ; les énumérations sont écrites en **texte** (`"Hard"` et non `1`). |
| `private readonly object _gate = new();` + `lock (_gate) { ... }` | Verrou : deux requêtes simultanées ne lisent/écrivent pas le fichier en même temps. |
| `var policy = ReadFromFile() ?? CreateDefault(); ConstraintCatalog.NormalizeSettings(policy); return Task.FromResult(policy);` | Lit le fichier ou prend les valeurs par défaut, **complète/corrige** les choix, renvoie un objet **neuf à chaque appel** (donc modifiable sans effet de bord). |
| `SaveAsync` : `var temporary = filePath + ".tmp"; File.WriteAllText(temporary, ...); File.Move(temporary, filePath, overwrite: true);` | Écrit d'abord un **fichier temporaire** puis le renomme : jamais de fichier à moitié écrit en cas de coupure. |
| `ResetAsync` : `File.Delete(filePath)` | Supprimer le fichier = revenir aux défauts. |
| `catch (JsonException) { return null; }` | **Fichier corrompu** : on retombe sur les valeurs par défaut au lieu de planter. |

## H2. `InMemory/DemoDataStore.cs` — les données de démonstration

Implémente **quatre** interfaces. Génère de façon **déterministe** (`new Random(date.DayNumber * 31 + line.LineId)`) 36 voyages par jour (8 lignes de 3 types), 12 bus (dont 3 confort, 2 articulés, 1 inactif), 14 chauffeurs et 14 receveurs (jours de repos répartis), une maintenance de bus (bus 2, 12 h–18 h) et un congé (chauffeur 105). Utile pour **comprendre et tester** sans base. `CreateDemoPolicy()` fournit les règles type de ligne -> catégories. Elle n'est **pas** utilisée en mode MySQL.

## H3. `MySql/MySqlTripRepository.cs` — **à adapter à vos données**

La requête de base (`BaseSql`) est une **chaîne brute** (`""" ... """`) qui peut s'étendre sur plusieurs lignes.

| Fragment SQL | Explication |
| --- | --- |
| `SELECT t.ID AS Id, t.DENUMLI AS LineId,` | Les alias (`AS Id`) correspondent **exactement** aux propriétés de la classe `Row` : c'est ainsi que Dapper associe colonnes et propriétés. |
| `COALESCE(l.DENOMLI, CONCAT('Ligne ', t.DENUMLI)) AS LineLabel` | Nom de la ligne ; si absent, « Ligne 12 ». `COALESCE` renvoie le premier argument non nul. |
| `l.DECTYLI AS LineTypeId` | **Type de ligne** (urbain, interurbain...) : sert à la compatibilité de catégorie et aux phases. |
| `t.TIME_DEPART AS Departure,` | Heure de départ prévue. |
| `(SELECT MAX(s.ARRIVAL_TIME) FROM stop_times s WHERE s.TRIP_ID = t.ID) AS Arrival` | **Heure d'arrivée = heure d'arrivée au dernier arrêt** (`trips` n'a pas d'heure d'arrivée). Sous-requête corrélée. |
| `COALESCE((SELECT MAX(s.SHAPE_DIST_TRAVELED) FROM stop_times s WHERE s.TRIP_ID = t.ID), l.DENBRKM, 0) AS DistanceKm` | Kilométrage : distance parcourue au dernier arrêt, sinon `DENBRKM` de la ligne, sinon 0. |
| `l.DECCENT AS CenterId, l.DECDELEG AS DelegationId` | Centre et délégation **de la ligne**. |
| `(SELECT s.DECSTAT FROM stop_times s WHERE s.TRIP_ID = t.ID ORDER BY s.STOP_SEQUENCE ASC LIMIT 1) AS OriginStationId` | Station du **premier** arrêt (et `DESC` pour le dernier = destination). Sert à BP05. |
| `t.BUS_PR AS BusId, t.CHAUFF_PR AS DriverId, t.REC_PR AS ReceiverId` | Ressources **déjà prévues** (null si non affectées). |
| `FROM trips t JOIN drligne l ON l.DENUMLI = t.DENUMLI AND l.DEACTIF = 1` | Seulement les voyages d'une **ligne active** (règle V02). Le `JOIN` élimine aussi les voyages sans ligne (V01). |
| `WHERE t.TIME_DEPART >= @From AND t.TIME_DEPART < @To` | Fenêtre de dates. `@From`/`@To` sont des **paramètres** (jamais de texte collé : protection contre l'injection SQL). |
| `AND COALESCE(t.ETAT, 0) <> 3` | Exclut les voyages **terminés** (3 = terminé d'après le commentaire de la colonne). **Les autres valeurs d'`ETAT` sont à confirmer** : c'est la règle V11 du document. |

| Code C# | Explication |
| --- | --- |
| `private sealed class Row { public long Id { get; set; } ... }` | Classe de lecture avec des propriétés **modifiables** (exigence de Dapper) et des types nullables pour les colonnes nullables. |
| `public Trip? ToTrip() => Arrival is { } arrival && arrival > Departure ? new Trip(...) : null;` | Convertit en `Trip`. Si l'arrivée est absente ou **incohérente** (règle V08), le voyage est **écarté** (`null`). **À journaliser en production** : un voyage écarté en silence est un voyage jamais planifié. |
| `await using var connection = await db.OpenAsync(ct);` | Ouvre une connexion et la **ferme automatiquement** à la fin du bloc. |
| `connection.QueryAsync<Row>(new CommandDefinition(BaseSql, new { From = ..., To = ... }, cancellationToken: ct))` | Exécute la requête. `new { From = ..., To = ... }` est un objet anonyme dont les propriétés deviennent les paramètres `@From`/`@To`. `to.AddDays(1)` rend la borne **exclusive** (le jour `to` entier est inclus). |
| `GetToAllocateAsync` : `.Where(r => r.BusId is null \|\| r.DriverId is null \|\| r.ReceiverId is null)` | Voyages **auxquels il manque au moins une ressource**. |
| `GetAssignedAsync` : `.Where(r => r.BusId is not null && ...)` puis `new AssignedTrip(trip, r.BusId!.Value, ...)` | Voyages **entièrement** affectés. `!` dit au compilateur « je sais que ce n'est pas null » ; `.Value` extrait la valeur du `long?`. |

**Points à adapter** : (1) **valeurs de `ETAT`** planifiables ; (2) **voyages partiellement affectés** (bus + chauffeur sans receveur) : aujourd'hui ils sont réaffectés en entier ; (3) **performance** : les sous-requêtes sur `stop_times` (700 000 lignes dans votre dump) s'appuient sur l'index `IDX_STOP_TIMES_TRIP_ID` ; ajoutez `CREATE INDEX IDX_TRIPS_TIME_DEPART ON trips (TIME_DEPART);`.

## H4. `MySql/MySqlResourceRepository.cs`

| Fragment | Explication |
| --- | --- |
| `QualificationOptions { public Dictionary<string, AgentRole> Map { get; set; } }` | **Mapping configurable** valeur de `dragent.DECQUAL` -> rôle. Exemple dans `appsettings.json` : `"Map": { "C": "Driver", "R": "Receiver" }` (valeurs **à confirmer**). **Le commentaire de `DECQUAL` dans votre dump est « Qualification (H=homme) » : il pourrait désigner un genre et non un rôle.** Sans mapping valide, tous les agents sont `Other` et rien ne s'alloue. |
| `SELECT DECODVH AS Id, DEMATRI AS Plate, COALESCE(DEACTIF, 0) AS Active, COALESCE(DENBRPA, 0) AS Seats, ... CAST(DECCENT AS SIGNED) AS CenterId, CAST(DECDELEG AS SIGNED) AS DelegationId, DEDATEC AS InServiceSince FROM drvehic` | Lecture des bus. **`CAST(... AS SIGNED)`** : dans `drvehic`, `DECCENT` et `DECDELEG` sont des `DOUBLE` alors que `dragent` et `drligne` utilisent des `BIGINT` : sans conversion, la comparaison de centres échouerait. |
| `.Select(r => new Bus(r.Id, r.Plate ?? $"#{r.Id}", r.Active == 1, (int)r.Seats, ...))` | Conversion ligne -> `Bus`. `r.Active == 1` transforme le 0/1 en booléen ; `(int)` convertit le `long` en `int`. |
| `SELECT DECAGEN AS Id, DENAGEN AS Name, DECQUAL AS Qual, DECJOUR AS RestDay, DECCENT AS CenterId, DECDELEG AS DelegationId FROM dragent` | Lecture des agents. La table `drdentr` (copie sans relation) est **ignorée**. |
| `r.Qual is not null && map.TryGetValue(r.Qual, out var role) ? role : AgentRole.Other` | Rôle d'après le mapping, sinon `Other`. |
| `private static DayOfWeek? ToDayOfWeek(long? decjour) => decjour is >= 1 and <= 7 ? (DayOfWeek)(decjour.Value % 7) : null;` | **Piège du jour de repos** : base 1 (lundi) à 7 (dimanche) ; .NET : dimanche = 0, lundi = 1... `7 % 7 = 0` -> dimanche ; `1 % 7 = 1` -> lundi. Valeurs hors 1..7 -> `null` (pas de jour de repos connu). `is >= 1 and <= 7` est un **motif relationnel**. |
| `SELECT KIND AS Kind, RESOURCE_ID AS ResourceId, START_AT AS StartAt, END_AT AS EndAt, REASON AS Reason FROM resource_unavailability WHERE END_AT > @From AND START_AT < @To` | Indisponibilités qui **touchent la fenêtre** (table à créer : `sql/001_allocation_tables.sql`). |

## H5. `MySql/MySqlAssignmentRepository.cs`

| Fragment | Explication |
| --- | --- |
| `await using var tx = await conn.BeginTransactionAsync(ct);` | Ouvre une **transaction** : tout ou rien. |
| `var rows = assignments.Select(a => new { TripId = a.Trip.Id, BusId = a.Bus.Id, ..., Score = Math.Round(a.Score, 2), a.Justification, User = user });` | Un objet anonyme par affectation, aux noms égaux aux paramètres SQL. |
| `INSERT INTO trip_assignments (TRIP_ID, BUS_ID, DRIVER_ID, RECEIVER_ID, STATUS, SCORE, JUSTIFICATION, ASSIGNED_AT, ASSIGNED_BY) VALUES (@TripId, ..., 'PROPOSED', ..., UTC_TIMESTAMP(), @User)` | Insère chaque proposition avec le statut `PROPOSED`. Avec une **liste** de paramètres, Dapper exécute l'insertion pour chaque élément. |
| `await tx.CommitAsync(ct);` | Valide **tout** d'un coup. En cas d'erreur (par exemple un voyage déjà affecté, bloqué par l'index unique `UX_ACTIVE_TRIP`), **toute la transaction est annulée**. |

`trips.BUS_PR`, `CHAUFF_PR` et `REC_PR` ne sont **pas** modifiés ici : on n'y copie qu'à la **validation** par un utilisateur (étape à ajouter chez vous).

## H6. `MySql/MySqlReferenceDataRepository.cs` et `MySqlConnectionFactory.cs`

`SELECT DECTYLI AS Id, DELTYLI AS Label FROM drtyli` et `SELECT DECATVH AS Id, DECATEG AS Label FROM drcatve` : alimentent les écrans de configuration. Libellé absent -> « Type de ligne 3 ». `MySqlConnectionFactory` ouvre une `MySqlConnection` à partir de la chaîne de connexion.

## H7. `DependencyInjection.cs`

| Méthode | Explication |
| --- | --- |
| `AddInMemoryInfrastructure(services, settingsFilePath)` | Mode **démo** : un seul `DemoDataStore` (singleton) enregistré sous **quatre** interfaces ; réglages dans le JSON avec la politique de démo par défaut. |
| `AddMySqlInfrastructure(services, connectionString, settingsFilePath, configureQualifications)` | Mode **MySQL** : fabrique de connexions + les quatre dépôts (`AddTransient`) ; `services.Configure(qualifications)` enregistre le mapping de qualifications. |

**Pour brancher une autre source de données** : créez vos implémentations des ports (G1) et une méthode d'enregistrement du même genre.

---

# PARTIE I — Web : l'interface Blazor

**Blazor Server** : les pages sont écrites en C# + HTML (fichiers `.razor`) et s'exécutent **sur le serveur**. Le navigateur reçoit le HTML et une connexion temps réel (SignalR) transmet les clics et met à jour l'affichage. Quand une variable de la page change, Blazor **redessine** ce qui en dépend.

## I1. `Program.cs` — le point de départ

| Instruction | Explication |
| --- | --- |
| `var builder = WebApplication.CreateBuilder(args);` | Prépare l'application web (configuration, journalisation, conteneur de services). |
| `builder.Services.AddRazorComponents().AddInteractiveServerComponents();` | Active les composants Razor en mode **serveur interactif**. |
| `builder.Services.AddApplication();` | Enregistre les cas d'usage (partie G). |
| `var settingsPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "allocation-settings.json");` | Chemin du fichier de réglages : dossier `App_Data` du projet Web. |
| `var connectionString = builder.Configuration.GetConnectionString("TransportAi");` | Lit `ConnectionStrings:TransportAi` dans `appsettings.json`. |
| `if (string.IsNullOrWhiteSpace(connectionString)) { builder.Services.AddInMemoryInfrastructure(settingsPath); builder.Services.AddSingleton(new DataSourceInfo(...)); }` | **Chaîne vide : mode démonstration.** `DataSourceInfo` est affichée sur la page d'accueil. |
| `else { builder.Services.AddMySqlInfrastructure(connectionString, settingsPath, options => builder.Configuration.GetSection("Qualifications").Bind(options)); ... }` | **Chaîne renseignée : MySQL.** `.Bind(options)` remplit `QualificationOptions` depuis la section `Qualifications` du fichier de configuration. |
| `var app = builder.Build();` | Fabrique l'application. |
| `app.UseStaticFiles();` | Sert le dossier `wwwroot` (`app.css`). |
| `app.UseAntiforgery();` | Protection contre les requêtes falsifiées (exigée par Blazor .NET 8). |
| `app.MapRazorComponents<App>().AddInteractiveServerRenderMode();` | Associe les adresses aux composants, à partir du composant racine `App`. |
| `app.Run();` | Démarre le serveur. |

**C'est ici que vous choisissez vos implémentations** (démo ou MySQL ou les vôtres) : le reste du code n'en dépend pas.

## I2. `Components/App.razor` — la page HTML racine

| Instruction | Explication |
| --- | --- |
| `<!DOCTYPE html> <html lang="fr">` | Document HTML en français. |
| `<base href="/" />` | Adresse de base pour les liens relatifs (`constraints`, `allocation`). |
| `<link rel="stylesheet" href="app.css" />` | Feuille de style de `wwwroot`. |
| `<HeadOutlet @rendermode="InteractiveServer" />` | Emplacement où les pages injectent leur titre (`<PageTitle>`). |
| `<Routes @rendermode="InteractiveServer" />` | Le routeur, en mode serveur interactif : **toutes** les pages sont interactives. |
| `<script src="_framework/blazor.web.js"></script>` | Script fourni par Blazor : ouvre la connexion temps réel. |

## I3. `Components/Routes.razor` et `_Imports.razor`

| Fichier / instruction | Explication |
| --- | --- |
| `<Router AppAssembly="typeof(Program).Assembly">` | Cherche les pages (`@page`) dans ce projet. |
| `<RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />` | Affiche la page trouvée dans la **mise en page** `MainLayout`. |
| `<FocusOnNavigate ... Selector="h1" />` | Après navigation, place le focus sur le titre (accessibilité). |
| `<NotFound>...</NotFound>` | Page affichée pour une adresse inconnue. |
| `_Imports.razor` : `@using ...` | Les `using` valables pour **tous** les fichiers `.razor` du dossier. `@using static Microsoft.AspNetCore.Components.Web.RenderMode` rend `InteractiveServer` utilisable. Les `@using SmartPlanif.Domain...` et `SmartPlanif.Application...` donnent accès aux types métier sans les répéter dans chaque page. |

## I4. Mise en page : `MainLayout.razor`, `NavMenu.razor`

| Instruction | Explication |
| --- | --- |
| `@inherits LayoutComponentBase` | Ce composant est une **mise en page**. |
| `<NavMenu />` | Le menu latéral. |
| `<main class="content">@Body</main>` | **`@Body` = le contenu de la page courante.** |
| `<NavLink href="constraints">Contraintes</NavLink>` | Lien qui reçoit automatiquement la classe CSS `active` quand on est sur cette page. `Match="NavLinkMatch.All"` pour l'accueil : actif **seulement** sur l'adresse exacte. |

## I5. `Pages/Home.razor`

`@page "/"` : adresse de la page. `@inject DataSourceInfo DataSource` : récupère le singleton enregistré dans `Program.cs`. `<div class="alert @(DataSource.IsDemo ? "warn" : "ok")">` : la classe CSS dépend d'une condition (jaune en démo, vert sinon). Le reste est du HTML statique (marche à suivre et principe).

## I6. `Pages/Constraints.razor` — choisir désactivée / dure / souple

### Le balisage

| Instruction | Explication |
| --- | --- |
| `@page "/constraints"` | Adresse. |
| `@using System.Globalization` | Pour `CultureInfo.InvariantCulture`. |
| `@inject AllocationSettingsService SettingsService` | Reçoit le service de réglages par injection. |
| `<PageTitle>Contraintes</PageTitle>` | Titre de l'onglet du navigateur. |
| `@if (_policy is null) { <p>Chargement…</p> } else { ... }` | Tant que la politique n'est pas chargée (opération asynchrone), on affiche « Chargement ». |
| `<span class="badge hard">@CountOf(ConstraintMode.Hard) dures</span>` | Compteurs : `@expression` insère le résultat d'une expression C#. |
| `@MaxScore.ToString("0.#", CultureInfo.InvariantCulture)` | Score maximal par voyage avec les choix en cours (somme des poids souples). Se met à jour à chaque changement. |
| `<button class="btn primary" @onclick="SaveAsync">Enregistrer</button>` | `@onclick` appelle la méthode C# au clic. |
| `@foreach (var target in new[] { ConstraintTarget.Bus, ConstraintTarget.Agent })` | Deux tableaux : bus, puis agents. |
| `@foreach (var descriptor in ConstraintCatalog.AllDescriptors.Where(d => d.Target == target))` | Une ligne par contrainte du catalogue. **L'interface se construit toute seule à partir du catalogue.** |
| `var setting = _policy.Constraints[descriptor.Code];` | Le choix actuel de cette contrainte. |
| `<tr class="@RowClass(setting.Mode)">` | Classe CSS selon le mode (bandeau rouge/bleu/grisé). |
| `@foreach (var mode in descriptor.SupportedModes) { var currentMode = mode; ... }` | Un bouton radio **par mode supporté**. `var currentMode = mode;` : copie locale, pour que la lambda `() => SetMode(..., currentMode)` garde la bonne valeur. |
| `<input type="radio" name="@("mode-" + descriptor.Code)" checked="@(setting.Mode == currentMode)" disabled="@descriptor.IsLocked" @onchange="() => SetMode(descriptor.Code, currentMode)" />` | `name` identique pour les radios d'une même contrainte (un seul coché) ; `checked` selon le choix courant ; `disabled` si verrouillée ; `@onchange` appelle `SetMode`. |
| `<input type="number" class="weight" value="@FormatWeight(setting.Weight)" disabled="@(descriptor.IsLocked \|\| setting.Mode != ConstraintMode.Soft)" @onchange="e => SetWeight(descriptor.Code, e.Value?.ToString())" />` | Champ de poids : **désactivé sauf en mode souple**. `e => ...` reçoit l'événement ; `e.Value?.ToString()` est le texte saisi. |

### Le code (`@code { ... }`)

| Instruction | Explication |
| --- | --- |
| `private AllocationPolicy? _policy;` | La politique en cours d'édition (copie en mémoire, pas encore enregistrée). |
| `private double MaxScore => _policy is null ? 0 : new TripAllocator(_policy).MaxScorePerTrip;` | Crée un allocateur juste pour lire son score maximal. |
| `protected override async Task OnInitializedAsync() => _policy = await SettingsService.LoadAsync();` | Appelée **à l'ouverture de la page** : charge la politique. |
| `private int CountOf(ConstraintMode mode) => _policy!.Constraints.Values.Count(s => s.Mode == mode);` | Compte les contraintes dans un mode. `!` : « je sais que `_policy` n'est pas nul ». |
| `SetMode(code, mode)` : `setting.Mode = mode; if (mode == Soft && setting.Weight <= 0) { var defaultWeight = ...DefaultWeight ?? 0; setting.Weight = defaultWeight > 0 ? defaultWeight : 10; }` | Change le mode. En passant en **souple** avec un poids nul, on remet le poids par défaut du catalogue (ou 10), sinon la règle serait sans effet. |
| `SetWeight(code, text)` : `double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var weight) && weight >= 0` | Lit le nombre saisi **en notation internationale** (point décimal) ; refuse les négatifs. `TryParse` ne plante pas sur un texte invalide. |
| `SaveAsync()` | Appelle `SettingsService.SaveAsync`, puis affiche le message de succès ou les erreurs. |
| `ResetAsync()` | Supprime le fichier de réglages puis recharge les valeurs par défaut. |
| `FormatWeight(double weight) => weight.ToString("0.##", CultureInfo.InvariantCulture);` | **Pourquoi `InvariantCulture` ?** En culture française, `12.5` s'écrirait `12,5` ; or un champ `type="number"` n'accepte que le **point**. Sans cela, les poids décimaux s'afficheraient vides. |
| `ModeLabel`, `ModeCss`, `RowClass` | Libellé français (« Désactivée / Dure / Souple »), classe CSS (`off/hard/soft`), classe de ligne. Les `switch` expressions : `mode switch { Disabled => ..., Hard => ..., _ => ... }` (`_` = tous les autres cas). |

**Comment l'écran « sait » quels modes proposer** : il lit `descriptor.SupportedModes` du catalogue. Rien n'est codé en dur dans la page.

## I7. `Pages/LineTypes.razor` — types de ligne et catégories de bus

| Instruction | Explication |
| --- | --- |
| `@inject IReferenceDataRepository Reference` | Pour lister les types de ligne et les catégories (depuis `drtyli` et `drcatve`). |
| `_lineTypes = await Reference.GetLineTypesAsync(CancellationToken.None);` | Chargement dans `OnInitializedAsync`. |
| Deux boucles imbriquées : types de ligne en lignes, catégories en cases à cocher | Colonne « autorisées » et colonne « préférées ». |
| `<input type="checkbox" checked="@IsAllowed(lineType.Id, category.Id)" @onchange="e => ToggleAllowed(lineType.Id, category.Id, IsChecked(e))" />` | Case cochée si la catégorie est dans la liste ; `IsChecked(e)` lit `e.Value is bool value && value`. |
| `GetOrCreateRule(lineTypeId)` | Retrouve la règle du type de ligne ou la **crée** et l'ajoute à `_policy.CategoryRules`. |
| `ToggleAllowed` | Cocher : ajoute à `AllowedCategoryIds` (sans doublon). **Décocher : retire aussi de `PreferredCategoryIds`** (une catégorie interdite ne peut pas être préférée). |
| `TogglePreferred` | Cocher : ajoute aux préférées ; ajoute **aussi aux autorisées seulement si une restriction existe déjà** (`AllowedCategoryIds.Count > 0`). **Pourquoi ?** Une liste « autorisées » vide veut dire « pas de restriction » : y ajouter une catégorie par erreur créerait une restriction que l'utilisateur n'a pas voulue. |
| `SaveAsync` : `_policy!.CategoryRules.RemoveAll(r => r.AllowedCategoryIds.Count == 0 && r.PreferredCategoryIds.Count == 0);` | Retire les règles **entièrement vides** avant d'enregistrer. |

## I8. `Pages/Parameters.razor`

| Instruction | Explication |
| --- | --- |
| `<input type="number" step="any" min="0" @bind="MaxDailyWorkHours" />` | `@bind` **lie** le champ à une propriété : la saisie met la propriété à jour, et inversement. `step="any"` autorise les décimales. |
| `private double MaxDailyWorkHours { get => _policy!.MaxDailyWork.TotalHours; set => _policy!.MaxDailyWork = TimeSpan.FromHours(value); }` | Propriété **de conversion** : la politique stocke un `TimeSpan`, l'interface manipule des heures (ou des minutes pour `MinAgentRestMinutes`...). |
| `@bind="_policy.MaxTripsPerAgentPerDay"` / `@bind="_policy.Alns.Seed"` | Liaison directe à une propriété de la politique. |

**Pour ajouter un paramètre** : un `<label>` + `<input @bind=...>` ici, la propriété dans `AllocationPolicy`, un contrôle dans `PolicyValidator`.

## I9. `Pages/Allocation.razor` — l'écran principal

### Les champs d'état

| Champ | Rôle |
| --- | --- |
| `DateTime _dateInput = new(2026, 10, 8);` | Date choisie. On utilise un `DateTime` car `@bind` avec `type="date"` le gère simplement ; `SelectedDate => DateOnly.FromDateTime(_dateInput)` le convertit. |
| `AllocationMethod _method` | Méthode choisie (3 par défaut). |
| `string _user`, `bool _saveProposals`, `bool _saveRequested` | Utilisateur, case « enregistrer », et mémorisation de la demande pour afficher l'avertissement si rien n'a été enregistré. |
| `bool _running`, `AlnsProgress? _progress`, `CancellationTokenSource? _cancellation` | État du calcul, dernière progression, jeton d'annulation. |
| `AllocationReport? _report`, `DayPreview? _preview`, `string? _error`, `string _filter` | Résultats, aperçu, message d'erreur, texte du filtre. |

### Les actions

| Méthode | Explication |
| --- | --- |
| `PreviewAsync()` | Appelle `PreviewHandler` ; stocke l'aperçu dans `_preview`. Une exception est capturée (`try/catch`) et affichée. |
| `RunAsync()` | `_running = true` (désactive les champs), crée `_cancellation = new CancellationTokenSource()`. |
| `var progress = new InlineProgress<AlnsProgress>(p => { _progress = p; _ = InvokeAsync(StateHasChanged); });` | **Reçoit la progression** de la métaheuristique, qui tourne sur **un autre fil**. `InvokeAsync(StateHasChanged)` demande à Blazor de redessiner **depuis le bon fil** (sans cela : erreur de fil). `_ =` ignore volontairement la valeur renvoyée. |
| `_report = await AllocateHandler.HandleAsync(command, progress, _cancellation.Token);` | Lance l'allocation. |
| `finally { _running = false; _cancellation?.Dispose(); _cancellation = null; }` | Quoi qu'il arrive : réactive l'interface et libère le jeton. |
| `Cancel() => _cancellation?.Cancel();` | Bouton Arrêter : l'ALNS rend le **meilleur plan trouvé**. |
| `FilteredAssignments()` | Filtre le tableau sur le texte saisi (ligne, plaque, nom d'agent) avec `Contains(text, StringComparison.OrdinalIgnoreCase)` (insensible à la casse). |

### Le balisage

- **Formulaire** : `<select @bind="_method">` avec `@foreach (var method in Enum.GetValues<AllocationMethod>())` : une `<option>` par méthode. Les champs ont `disabled="@_running"`.
- **Bandeau de progression** (`@if (_running)`) : itération, meilleur objectif, voyages non affectés, durée.
- **Aperçu** : `RenderFeasibility(...)` affiche le tableau de faisabilité (lignes rouges si insuffisant).
- **Indicateurs (KPI)** : affectés/total, non affectés, objectif construction -> final, durée, validité. La classe `good`/`bad` colore selon le résultat.
- **Contraintes appliquées** : badges rouges (dures) et bleus (souples), avec le nom complet en info-bulle (`title="@ConstraintCatalog.DescribeCode(...)"`).
- **Violations** : encadré rouge si le contrôle indépendant en trouve (devrait rester vide).
- **Phases de construction**, **ALNS** (courbe `<ConvergenceChart Points="_report.Alns.Convergence" />` et statistiques d'opérateurs).
- **Voyages non affectés** : causes de rejet traduites par `Causes(failure)` : `string.Join(" ; ", ...OrderByDescending(x => x.Value).Select(x => $"{ConstraintCatalog.DescribeCode(x.Key)} × {x.Value}"))` (les causes les plus fréquentes d'abord).
- **Plan d'allocation** : tableau filtrable ; `title="@a.Justification"` affiche la **justification** au survol ; la barre de score utilise `style="width:@Percent(a.Score, a.MaxScore)%"`.
- `RenderFeasibility` et `RenderFeasibilityIfProblem` : méthodes qui **renvoient du balisage** (`RenderFragment`). Elles évitent de dupliquer le tableau de faisabilité.

## I10. `Shared/ConvergenceChart.razor` — la courbe

| Instruction | Explication |
| --- | --- |
| `<svg viewBox="0 0 620 240" ...>` | Zone de dessin vectoriel de 620 × 240 unités (s'adapte à la largeur de l'écran). |
| `[Parameter] public IReadOnlyList<ConvergencePoint> Points { get; set; }` | **Paramètre du composant** : la page lui passe les points. |
| `OnParametersSet()` | Appelée à chaque changement de paramètre : recalcule la polyligne. |
| `_min`, `_max`, `_lastIteration` | Bornes pour mettre à l'échelle. |
| `var x = Left + (Right - Left) * p.Iteration / _lastIteration;` | Abscisse proportionnelle à l'itération. |
| `var y = range <= 0 ? (Top + Bottom) / 2 : Bottom - (Bottom - Top) * (p.BestObjective - _min) / range;` | Ordonnée : en SVG, y croît **vers le bas**, d'où `Bottom - ...` : un meilleur objectif (plus petit) est dessiné **plus bas**. Si la courbe est plate (`range <= 0`), on la trace au milieu. |
| `string.Create(CultureInfo.InvariantCulture, $"{x:0.##},{y:0.##}")` | Coordonnées avec **point décimal** (le SVG n'accepte pas la virgule). |
| `<polyline points="@_polyline" .../>` | Trace la courbe. |

## I11. `Services/UiSupport.cs`, `appsettings.json`, `app.css`

- `DataSourceInfo(string Description, bool IsDemo)` : texte de la source de données affiché sur l'accueil.
- `InlineProgress<T>(Action<T> handler) : IProgress<T>` : appelle **directement** le délégué dans `Report`. (La classe standard `Progress<T>` repasse par le contexte de synchronisation, ce qui peut retarder ou perdre des mises à jour dans Blazor Server.)
- `appsettings.json` : `ConnectionStrings:TransportAi` (vide = démo) et `Qualifications:Map` (valeur de `DECQUAL` -> `Driver`/`Receiver`).
- `app.css` : feuille de style unique, sans dépendance externe ; variables CSS (`--hard`, `--soft`...) pour les couleurs ; `.badge.hard/.soft`, `.alert.ok/.warn/.error`, `table.grid`, `.kpi`, `.scorebar`.

**Pour modifier l'interface** : couleurs dans `:root` de `app.css` ; textes dans les `.razor` ; nouvelle page = nouveau fichier `.razor` avec `@page "/ma-page"` + un `<NavLink>` dans `NavMenu.razor`.

---

# PARTIE J — Les tests (`tests/SmartPlanif.Domain.Tests`)

Un test est une petite méthode marquée `[Fact]` qui **prépare une situation, exécute le code, vérifie le résultat** (`Assert.Equal`, `Assert.True`...). Les tests sont votre **filet de sécurité** : après chaque modification, `dotnet test` doit rester vert.

## J1. `TestData.cs` — les fabriques

| Méthode | Rôle |
| --- | --- |
| `Day` | Jeudi 8 octobre 2026 : jour de référence. |
| `Trip(id, fromHour, toHour, center, line, lineType)` | Fabrique un voyage : `Day.AddHours(fromHour)` pour le départ. `Trip(1, 6, 8)` = de 06:00 à 08:00. |
| `Bus(id, active, center, category)` / `Agent(id, role, restDay, center)` | Fabriquent un bus ou un agent avec des valeurs par défaut. |
| `Policy(Action<AllocationPolicy>? configure)` | Politique par défaut, modifiable par une action, puis `NormalizeSettings`. |
| `SetMode(policy, code, mode, weight)` | Change le mode d'une contrainte. |
| `Run(trips, buses, agents, policy?, unavailabilities?)` | Allocation gloutonne complète. |

## J2. `ConstraintTests.cs` — règles et choix de contraintes

| Test | Ce qu'il prouve |
| --- | --- |
| `Agent_on_rest_day_is_excluded` | Un chauffeur en repos le jeudi n'est pas choisi. |
| `Inactive_bus_is_excluded` | Un bus inactif n'est jamais choisi. |
| `Bus_in_maintenance_is_excluded` | Une maintenance qui recouvre le voyage exclut le bus. |
| `Overlapping_trips_cannot_share_the_only_bus` | Deux voyages qui se chevauchent avec **un seul** bus : un des deux reste non affecté, avec la cause « B03 ». |
| `Daily_work_limit_is_enforced_by_default` | 4 h + 5 h pour un seul chauffeur dépasse 8 h : cause « A08 ». |
| `Disabling_a_constraint_removes_its_effect` | Avec A08 **désactivée**, le même scénario réussit. |
| `Locked_constraint_cannot_be_disabled` | Tenter de désactiver B03 est **sans effet**. |
| `Same_center_as_soft_rule_prefers_but_does_not_exclude` | En souple, le même centre est **préféré** mais un autre centre reste accepté s'il n'y a pas le choix. |
| `Same_center_as_hard_rule_excludes_other_centers` | En dur, un bus d'un autre centre est **rejeté** (cause « B07 »). |
| `Intercity_line_requires_comfort_bus_when_category_rule_is_hard` | La règle de catégorie impose un bus confort sur le type de ligne 2 ; sans bus confort : cause « B06 ». |
| `Disabling_category_rule_ignores_the_matrix` | B06 désactivée : la matrice est ignorée. |
| `Preferred_category_keeps_comfort_buses_for_long_lines` | Avec le bus confort ayant l'identifiant **le plus petit**, seul le critère BP03 peut faire choisir le standard : il le fait. |
| `Constraint_catalog_normalizes_invalid_settings` | Un mode non supporté ou verrouillé est **corrigé**. |
| `Policy_validator_reports_incoherent_values` | Refroidissement 1,5 et km = 0 sont signalés ; la politique par défaut est valide. |

## J3. `AllocationStrategyTests.cs` — stratégies, ALNS, validateur

`RandomScenario(tripCount, seed)` génère un jeu aléatoire **reproductible** (9 bus, 9 chauffeurs, 9 receveurs, voyages aléatoires).

| Test | Ce qu'il prouve |
| --- | --- |
| `Greedy_result_is_deterministic` | Deux exécutions identiques donnent le même plan. |
| `Plan_never_has_overlapping_trips_for_any_resource` | **Test d'invariant** : sur 60 voyages, aucune ressource n'a deux voyages qui se chevauchent. Il est écrit **sans** utiliser les règles : il ne dépend pas du code testé. |
| `Phased_construction_gives_scarce_comfort_bus_to_the_line_that_needs_it` | Le glouton chronologique laisse **1** voyage sans bus ; la construction par phases les affecte **tous** et traite en premier le type de ligne le plus tendu. |
| `Alns_never_degrades_the_plan_and_result_is_valid` | L'ALNS rend un objectif **≤** à celui de départ, jamais plus de voyages non affectés, et **aucune violation**. |
| `Alns_is_reproducible_with_the_same_seed` | Même graine = même plan final. |
| `Alns_stops_when_cancellation_is_requested_and_still_returns_a_plan` | Annulation avant démarrage : 0 itération, mais le plan initial est rendu. |
| `Validator_detects_a_plan_that_violates_a_hard_rule` | On **truque à la main** un plan (un bus sur deux voyages qui se chevauchent) : le validateur le détecte (« B03 »). |
| `Final_scores_are_recomputed_exactly_and_explained` | Les justifications existent et aucun score ne dépasse le maximum. |
| `Feasibility_analysis_flags_a_bus_shortage` | 3 voyages simultanés et 2 bus : demande 3, offre 2, insuffisant. |

## J4. `SettingsStoreTests.cs`

| Test | Ce qu'il prouve |
| --- | --- |
| `Policy_survives_a_save_and_load_round_trip` | On enregistre (B07 dure, AP04 souple à 33, 7,5 h), on recharge : tout est retrouvé. `Path.GetTempPath()` + un GUID : fichier temporaire unique, supprimé dans `finally`. |
| `Corrupted_file_falls_back_to_defaults` | Un fichier JSON invalide ne plante pas ; on retrouve 8 h et toutes les contraintes. |

**Écrire un test pour votre nouvelle règle** : copiez un test existant, changez la situation (un voyage, un bus, un agent) et l'assertion attendue.

---

# PARTIE K — Intégrer à vos données réelles et à votre dépôt GitHub

> Je n'ai pas pu lire `https://github.com/salah1892/SmartPlanif` : cette partie est donc générale. Envoyez-moi l'arborescence (dossiers, projets, versions de .NET, ORM utilisé) et je l'adapterai exactement.

## K1. Deux façons d'intégrer

| Cas | Que faire |
| --- | --- |
| **Votre dépôt est vide ou presque** | Copiez les dossiers `src/`, `tests/`, `sql/` et `SmartPlanif.sln` à la racine du dépôt. Les projets sont déjà prêts. |
| **Votre solution a déjà ses propres projets** (entités, EF Core, API, interface) | Copiez **uniquement `SmartPlanif.Domain` et `SmartPlanif.Application`** (aucune dépendance externe, sauf l'abstraction d'injection de dépendances). Puis écrivez **vos propres implémentations des ports** (`ITripRepository`, `IResourceRepository`, `IAssignmentRepository`, `IReferenceDataRepository`, `IAllocationSettingsStore`) au-dessus de vos classes ou de votre `DbContext`. **Domain et Application ne changent pas.** |

**Règle d'or** : n'importez jamais vos entités de base de données dans le Domain. Convertissez-les en `Trip`, `Bus`, `Agent` **dans le dépôt** (c'est le rôle de l'Infrastructure).

## K2. Ordre recommandé, étape par étape

1. **Faire passer la solution telle quelle** : `dotnet build`, `dotnet test`, `dotnet run --project src\SmartPlanif.Web`. Vérifiez que la démo fonctionne chez vous.
2. **Lire le guide** dans l'ordre : parties A à F (le moteur), puis G à I.
3. **Préparer une copie de la base** (jamais la production) et exécuter `sql/001_allocation_tables.sql`.
4. **Configurer la connexion** sans la mettre dans Git (voir K4). Renseigner `Qualifications:Map` : **découvrez d'abord ce que contient `DECQUAL`** (`SELECT DECQUAL, COUNT(*) FROM dragent GROUP BY DECQUAL;`) et identifiez avec la société comment distinguer chauffeurs et receveurs.
5. **Valider la lecture avant tout calcul** : écrire un petit programme (ou un test d'intégration) qui appelle les trois dépôts pour **une journée réelle** et affiche : nombre de voyages lus, voyages écartés (arrivée absente ou incohérente), bus actifs, agents par rôle (`Driver`, `Receiver`, `Other`), indisponibilités. **Si `Other` contient presque tous les agents, le mapping est faux.**
6. **Lancer en simulation** (case « Enregistrer » décochée) avec la méthode 1 (glouton) : lire le **diagnostic de faisabilité** et les **causes de rejet**. Corriger les données ou les paramètres avant de chercher à optimiser.
7. **Valider les paramètres métier** avec la société : 8 h (conduite, présence ou amplitude ?), repos minimal, remise en service, jour de repos, congés.
8. **Comparer les trois méthodes** sur plusieurs journées (nombre de voyages affectés, objectif, temps).
9. **Activer l'enregistrement** (`PROPOSED`), puis ajouter l'étape de **validation** qui copie vers `trips.BUS_PR`, `CHAUFF_PR`, `REC_PR` et met le statut à `VALIDATED`.
10. **Intégrer l'utilisateur connecté** (`ASSIGNED_BY`) : votre base contient déjà `transportai_users`.

## K3. Contrôle de qualité de vos données (à faire **avant** de blâmer l'algorithme)

| Vérification | Pourquoi |
| --- | --- |
| Voyages sans arrêts dans `stop_times` ou avec `ARRIVAL_TIME` nul | Ils sont **écartés** par `ToTrip()` : ils ne seront jamais planifiés. |
| Valeurs de `trips.ETAT` | Seul `3 = terminé` est exclu ; les autres valeurs (annulé ? clôturé ?) sont à confirmer. |
| Valeurs de `dragent.DECQUAL` et de `DECJOUR` | Rôles et jour de repos (1 à 7 ?). |
| Types de `DECCENT` / `DECDELEG` | `drvehic` : `DOUBLE` ; `dragent`, `drligne` : `BIGINT` (déjà converti par `CAST`). |
| Contenu de `drtyli` et `drcatve` | Définit ce qu'on peut cocher dans « Types de ligne ». |
| `drligne.DEACTIF`, `drvehic.DEACTIF` | Définition de « actif ». |
| Doublons agents (`dragent` / `drdentr`) | Quelle table est la référence ? |
| Voyages aller-retour (`HAVERET`, `TRIP_NID`) | Traités comme **indépendants** : à confirmer. |
| Voyages qui passent minuit | Les jours sont comptés au **jour du départ**. |

## K4. Bonnes pratiques GitHub

- **Ne commitez jamais** un mot de passe. Pour la chaîne de connexion en développement : `dotnet user-secrets init --project src\SmartPlanif.Web` puis `dotnet user-secrets set "ConnectionStrings:TransportAi" "Server=...;Database=transport_ai;User=...;Password=..." --project src\SmartPlanif.Web`. En production : variable d'environnement `ConnectionStrings__TransportAi`.
- **`.gitignore`** : au minimum `bin/`, `obj/`, `.vs/`, `src/SmartPlanif.Web/App_Data/allocation-settings.json` (réglages locaux).
- **Branches** : une branche par évolution (`feature/regle-habilitation`), fusion après `dotnet test` vert.
- **Intégration continue** (GitHub Actions) : `dotnet build` puis `dotnet test` à chaque `push`.
- **Messages de commit** clairs : « Ajoute la règle A14 habilitation chauffeur + tests ».

## K5. Avant la mise en production

- **Authentification et droits** : l'application Blazor fournie **n'a aucune authentification**. Reliez-la à votre système d'utilisateurs avant tout usage réel.
- **Journalisation** : tracez les voyages écartés, les allocations lancées, les violations détectées.
- **Concurrence** : l'index unique `UX_ACTIVE_TRIP` protège contre deux allocations simultanées du même voyage ; prévoyez un message clair en cas d'échec.
- **Volumes** : testez avec une journée complète réelle. Si les temps sont trop longs, diminuez `MaxIterations`/`MaxSeconds` ou optimisez `ResourceSchedule` (recherche binaire).
- **Sauvegardes** : exécutez d'abord toutes les étapes sur une copie de la base.

---

# PARTIE L — Recettes de modification et dépannage

## L1. « Je veux... -> je modifie... »

| Je veux... | Où | Comment |
| --- | --- | --- |
| Changer 8 h en 7 h 30 | Page Paramètres (ou `AllocationPolicy.MaxDailyWork`) | Saisir 7,5 et enregistrer. |
| Rendre le centre **obligatoire** | Page Contraintes | Passer B07 et A11 en **Dure**. |
| Donner plus d'importance à l'équité | Paramètres ALNS | Augmenter `FairnessWeight`. |
| Ne plus se soucier de l'ancienneté des bus | Page Contraintes | Désactiver BP08. |
| Interdire le bus articulé sur une ligne | Page Types de ligne | Décocher « articulé » dans « autorisées » (avec B06 en dur). |
| Ajouter une règle | `Rules/` + `ConstraintCatalog` | Voir C6. |
| Qu'un chauffeur ne fasse jamais plus de 5 h de conduite d'affilée | Nouvelle règle dure `IHardRule<Agent>` | Utiliser `Schedule.Previous(...)` en boucle ou ajouter une méthode `ConsecutiveWork(...)` à `ResourceSchedule`. |
| Compter les pauses dans le temps de travail | `ResourceSchedule.WorkedOn` | Changer le calcul à cet endroit. |
| Accepter une affectation partielle | `TripAllocator.TryAssign` | Modifier la condition `bus is null \|\| driver is null \|\| receiver is null`. |
| Utiliser vos propres tables | Infrastructure | Réécrire les dépôts (partie H) ; ne rien changer ailleurs. |
| Un nouvel écran | `Components/Pages` | Voir I11. |
| Plus de temps de recherche | Paramètres ALNS | `MaxIterations` et `MaxSeconds`. |
| Un résultat différent à chaque lancement | Paramètres ALNS | Changer `Seed` (même graine = même résultat). |

## L2. Dépannage

| Symptôme | Cause probable | Où regarder |
| --- | --- | --- |
| **Aucun voyage affecté** sur MySQL, causes « DRIVER, RECEIVER » | Tous les agents sont `Other` : mapping `DECQUAL` absent ou faux | `Qualifications:Map` ; étape K2.5. |
| « 0 voyages à affecter » | Fenêtre de dates, `ETAT`, ou ligne inactive | `BaseSql` de `MySqlTripRepository`. |
| Voyages manquants | Arrivée nulle ou incohérente (écartés) | `Row.ToTrip()` : ajouter un journal. |
| Un voyage reste non affecté | Lire la colonne « Causes de rejet » et le diagnostic de faisabilité | `ConstraintCatalog.DescribeCode`. |
| Les poids décimaux ne s'affichent pas | Culture française et `type="number"` | Utiliser `FormatWeight` (déjà en place). |
| « Violations détectées » après l'ALNS | Bug dans un opérateur ou une règle | Les tests d'invariant ; ne **jamais** enregistrer (le handler le refuse). |
| La métaheuristique n'améliore rien | Peu d'itérations, ou le glouton est déjà proche de l'optimum | Statistiques d'opérateurs, courbe de convergence. |
| La page « Types de ligne » est vide | Les tables `drtyli`/`drcatve` sont vides ou la requête échoue | `MySqlReferenceDataRepository`. |
| `The 'code' directive must appear at the start of the line` | Une variable nommée `code` suivie de `@code` dans un `.razor` | Renommer la variable. |

## L3. Plan d'étude proposé (5 exercices)

1. **Lire et lancer** : exécutez `dotnet test`, lisez les noms des tests : ils décrivent le comportement attendu.
2. **Jouer avec les poids** : page Contraintes, changez B07 de souple à dure, relancez, observez les causes de rejet.
3. **Écrire une règle souple** (la plus simple) : par exemple « préférer le bus ayant le moins de places » ; classe + ligne dans le catalogue + test.
4. **Écrire une règle dure** : par exemple « un agent ne travaille pas plus de 3 voyages consécutifs » ; observez l'effet sur le diagnostic.
5. **Ajouter un opérateur ALNS** : copiez `RandomRemoval`, changez le critère de sélection, comparez les statistiques d'opérateurs.

---

# Annexe — Glossaire

| Terme | Définition |
| --- | --- |
| **Règle dure** | Condition obligatoire : si elle est violée, la ressource est éliminée. |
| **Règle souple** | Critère de préférence : ajoute des points au score. |
| **Score** | Somme (poids × valeur entre 0 et 1) des critères souples. |
| **Heuristique** | Méthode rapide qui construit une bonne solution sans garantir l'optimum. |
| **Métaheuristique** | Stratégie générale d'amélioration (ici ALNS + recuit simulé). |
| **ALNS** | Adaptive Large Neighborhood Search : détruire/réparer avec des opérateurs adaptatifs. |
| **Recuit simulé** | Accepter parfois une solution moins bonne, de moins en moins avec la température. |
| **Objectif** | Nombre à minimiser qui mesure la qualité d'un plan. |
| **Faisabilité** | Existence d'au moins un plan respectant les règles dures. |
| **Tension** | Demande maximale simultanée / ressources compatibles. |
| **Port** | Interface que l'extérieur (base, fichier) doit implémenter. |
| **Adaptateur** | Implémentation concrète d'un port (MySQL, démo, JSON). |
| **Record** | Type de données immuable avec égalité par valeur. |
| **Injection de dépendances** | Les classes déclarent leurs besoins ; `Program.cs` fournit les implémentations. |
| **Déterministe** | Mêmes entrées = mêmes sorties. |
