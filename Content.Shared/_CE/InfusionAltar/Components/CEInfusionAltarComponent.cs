using Robust.Shared.Map;
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
    /// How often to re-check recipe conditions.
    /// </summary>
    [DataField]
    public TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

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
}
