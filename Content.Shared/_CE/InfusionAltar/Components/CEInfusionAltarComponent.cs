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
    /// Current instability meter (0..<see cref="MaxInstability"/>). Its fraction of
    /// <see cref="MaxInstability"/> determines the altar's danger level (Calm/Unstable/Critical),
    /// which drives both the mishap roll chance and the danger visual band.
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
    /// Mishap roll chance per tick while at <see cref="Content.Shared._CE.InfusionAltar.CEInfusionAltarDangerLevel.Unstable"/>.
    /// Calm never rolls (0%); Critical uses <see cref="CriticalMishapChance"/> instead.
    /// </summary>
    [DataField]
    public float UnstableMishapChance = 0.05f;

    /// <summary>
    /// Mishap roll chance per tick while at <see cref="Content.Shared._CE.InfusionAltar.CEInfusionAltarDangerLevel.Critical"/>.
    /// </summary>
    [DataField]
    public float CriticalMishapChance = 0.15f;

    /// <summary>
    /// Instability growth per second while powered, no catalyst inserted.
    /// </summary>
    [DataField]
    public float PoweredIdleInstabilityRate = 0.17f;

    /// <summary>
    /// Instability growth per second whenever the ritual
    /// isn't actively progressing while powered with a catalyst inserted: either no recipe's
    /// catalyst requirement matches it (an attempt that can never succeed), or a recipe is
    /// identified but its conditions are currently unmet (missing essence/pedestal items).
    /// </summary>
    [DataField]
    public float BrokenConditionInstabilityRate = 0.67f;

    /// <summary>
    /// Instability decay per second while unpowered.
    /// </summary>
    [DataField]
    public float UnpoweredDecayRate = 0.83f;

    /// <summary>
    /// Pool of mishap entity prototypes rolled while at <see cref="Content.Shared._CE.InfusionAltar.CEInfusionAltarDangerLevel.Unstable"/>.
    /// Also rolled from at <see cref="Content.Shared._CE.InfusionAltar.CEInfusionAltarDangerLevel.Critical"/>, alongside
    /// <see cref="CriticalMishaps"/>. Each mishap entity carries its own effect components (damage,
    /// area effects, item ejection, etc.) rather than the altar hardcoding effect logic.
    /// </summary>
    [DataField]
    public List<EntProtoId> UnstableMishaps = new();

    /// <summary>
    /// Pool of mishap entity prototypes only rolled while at <see cref="Content.Shared._CE.InfusionAltar.CEInfusionAltarDangerLevel.Critical"/>,
    /// on top of <see cref="UnstableMishaps"/> - reserved for the most severe effects.
    /// </summary>
    [DataField]
    public List<EntProtoId> CriticalMishaps = new();
}
