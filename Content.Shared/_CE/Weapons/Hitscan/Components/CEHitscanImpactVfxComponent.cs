using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Weapons.Hitscan.Components;

/// <summary>
///     Generic marker for hitscan entities (ammo/laser prototypes) that should spawn a VFX entity at the
///     actual impact point when fired. Reusable by any hitscan, not just third arm modules.
/// </summary>
[RegisterComponent]
public sealed partial class CEHitscanImpactVfxComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Vfx;
}
