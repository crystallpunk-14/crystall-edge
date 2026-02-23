using Content.Shared._White.StatusEffect.Systems;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.StatusEffect.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(WhiteDamageModifierStatusEffectSystem))]
public sealed partial class WhiteDamageModifierStatusEffectComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, float>? Defence;

    [DataField]
    public float GlobalDefence = 1f;
}
