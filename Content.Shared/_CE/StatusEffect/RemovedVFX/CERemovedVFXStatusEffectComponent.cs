using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.StatusEffect.RemovedVFX;

/// <summary>
/// Spawns a VFX entity and plays a sound at the target when this status effect is removed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CERemovedVFXStatusEffectComponent : Component
{
    [DataField]
    public EntProtoId? Vfx;

    [DataField]
    public SoundSpecifier? Sound;
}
