using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.StatusEffect.VFX;

/// <summary>
/// Spawns a VFX entity and plays a sound at the target when this status effect is applied and/or removed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CEVFXStatusEffectComponent : Component
{
    [DataField]
    public EntProtoId? AppliedVfx;

    [DataField]
    public SoundSpecifier? AppliedSound;

    [DataField]
    public EntProtoId? RemovedVfx;

    [DataField]
    public SoundSpecifier? RemovedSound;
}
