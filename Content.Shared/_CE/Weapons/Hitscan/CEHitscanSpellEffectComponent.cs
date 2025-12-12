using Content.Shared._CE.Actions.Spells;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Weapons.Hitscan;

[RegisterComponent, NetworkedComponent,
 Access(typeof(CEHitscanSpellEffectSystem))]
public sealed partial class CEHitscanSpellEffectComponent : Component
{
    [DataField(required: true)]
    public List<CESpellEffect> Effects = new();

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}
