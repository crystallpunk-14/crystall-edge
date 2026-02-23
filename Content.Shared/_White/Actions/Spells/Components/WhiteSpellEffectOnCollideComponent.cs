using Content.Shared._CE.Actions.Spells;
using Content.Shared.Whitelist;

namespace Content.Shared._White.Actions.Spells.Components;

/// <summary>
/// Component that allows an meleeWeapon to apply effects to other entities on melee attacks.
/// </summary>
[RegisterComponent]
public sealed partial class WhiteSpellEffectOnCollideComponent : Component
{
    [DataField(required: true, serverOnly: true)]
    public List<CESpellEffect> Effects = new();

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public float Prob = 1f;
}
