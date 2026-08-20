using Content.Shared._CE.EntityEffect;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._CE.Weapons.Hitscan;

[RegisterComponent, NetworkedComponent,
 Access(typeof(CEHitscanSpellEffectSystem))]
public sealed partial class CEHitscanEntityEffectComponent : Component
{
    [DataField(required: true)]
    public List<CEEntityEffect> Effects = new();

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}
