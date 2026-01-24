using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.StatusEffect;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CEDamageModifierStatusEffectSystem))]
public sealed partial class CEDamageModifierStatusEffectComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, float>? Defence = null;

    [DataField]
    public float GlobalDefence = 1f;
}
