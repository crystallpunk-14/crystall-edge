using Content.Shared.Damage;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.OreVein;

/// <summary>
/// When the specified type of damage accumulates, the specified entity is created and the damage is reset.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CEOreVeinSystem))]
public sealed partial class CEOreVeinComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = new();

    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    [DataField]
    public SoundSpecifier SpawnSound = new SoundPathSpecifier("/Audio/Effects/picaxe2.ogg");
}
