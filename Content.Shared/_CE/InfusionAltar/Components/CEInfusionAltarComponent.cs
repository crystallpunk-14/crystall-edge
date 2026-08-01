using Content.Shared._CE.InfusionAltar.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._CE.InfusionAltar.Components;

/// <summary>
/// Marks the central pedestal ("altar") of an infusion altar setup. Server-side, while powered, it
/// periodically checks whether the single item inserted into its "catalyst" ItemSlots slot plus the
/// essence pooled in <see cref="Solution"/> satisfy any known recipe. Shared so the client can read
/// <see cref="PossiblePedestalsPositions"/> for the examine indicator overlay.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEInfusionAltarComponent : Component
{
    /// <summary>
    /// How often the ritual tick runs (recipe resolution, progress, instability growth, mishap roll).
    /// </summary>
    [DataField]
    public TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Next time <see cref="CheckInterval"/> allows a recheck.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextCheckTime = TimeSpan.Zero;

    /// <summary>
    /// The solution essence is drained from/into, matching the pedestal's <c>CEMagicEssenceAttractor</c> solution.
    /// </summary>
    [DataField]
    public string Solution = "essence";

    /// <summary>
    /// Tile offsets (relative to this altar) that are valid positions for a sub-pedestal. Scanned to
    /// find placed sub-pedestals, and shown as temporary indicators when the altar is examined.
    /// </summary>
    [DataField]
    public HashSet<Vector2i> PossiblePedestalsPositions = new();

    /// <summary>
    /// Sub-pedestals currently anchored at one of <see cref="PossiblePedestalsPositions"/>. Maintained by
    /// <see cref="Content.Server._CE.InfusionAltar.CEInfusionAltarSystem"/> in response to anchor changes
    /// on this altar and on <see cref="CEInfusionAltarPedestalComponent"/> entities.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> ConnectedPedestals = new();

    /// <summary>
    /// Current instability meter (0..<see cref="MaxInstability"/>). Drives mishap roll chance
    /// (<c>Instability / InstabilityDivisor</c> per tick) and the danger visual band.
    /// </summary>
    [DataField]
    public float Instability;

    [DataField]
    public float MaxInstability = 100f;

    /// <summary>
    /// How long the current ritual's conditions have been continuously satisfied. Only advances while
    /// <see cref="AttemptingRecipe"/>'s conditions all hold; paused (not reset) otherwise.
    /// </summary>
    [DataField]
    public TimeSpan RitualProgress = TimeSpan.Zero;

    /// <summary>
    /// The recipe currently identified by the catalyst, chosen among catalyst-matching candidates by
    /// which one currently has the most satisfied conditions. Null if no catalyst, or no recipe's
    /// catalyst requirement matches it.
    /// </summary>
    [DataField]
    public ProtoId<CEInfusionAltarRecipePrototype>? AttemptingRecipe;

    /// <summary>
    /// Mishap roll chance per tick = <c>Instability / InstabilityDivisor</c>.
    /// </summary>
    [DataField]
    public float InstabilityDivisor = 500f;

    /// <summary>
    /// Instability growth per second while powered, no catalyst inserted.
    /// </summary>
    [DataField]
    public float PoweredIdleInstabilityRate = 0.17f;

    /// <summary>
    /// Instability growth per second while powered, catalyst inserted but no recipe's catalyst
    /// requirement matches it - an attempt that can never succeed.
    /// </summary>
    [DataField]
    public float InvalidCatalystInstabilityRate = 0.67f;

    /// <summary>
    /// Instability growth per second while a recipe is identified but its conditions are currently
    /// unmet (missing essence/pedestal items).
    /// </summary>
    [DataField]
    public float BrokenConditionInstabilityRate = 1.33f;

    /// <summary>
    /// Instability decay per second while unpowered.
    /// </summary>
    [DataField]
    public float UnpoweredDecayRate = 0.83f;

    /// <summary>
    /// Pool of mishap entity prototypes. A random entry is spawned whenever the per-tick mishap roll
    /// succeeds; each mishap entity carries its own effect components (damage, area effects, item
    /// ejection, etc.) rather than the altar hardcoding effect logic.
    /// </summary>
    [DataField]
    public List<EntProtoId> Mishaps = new();

    /// <summary>
    /// Cached multiplier applied to instability growth, recomputed periodically by
    /// <see cref="Content.Server._CE.InfusionAltar.CEInfusionAltarSystem"/>'s stabilizer scan. Symmetric
    /// pairs of nearby <see cref="CEInfusionAltarStabilizerComponent"/> items reduce it, unpaired ones
    /// raise it.
    /// </summary>
    [DataField]
    public float StabilizerFactor = 1f;

    [DataField]
    public float StabilizerScanRadius = 6f;

    [DataField]
    public TimeSpan StabilizerScanInterval = TimeSpan.FromSeconds(5);

    [DataField, AutoPausedField]
    public TimeSpan NextStabilizerScan = TimeSpan.Zero;
}
