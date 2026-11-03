using Content.Shared.Damage;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.OreVein;

/// <summary>
/// When the specified type of damage accumulates from a single source, the specified entity is created near that source and its progress is reset.
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

    /// <summary>
    /// Damage accumulated by each damage source. Server only.
    /// </summary>
    [DataField]
    public Dictionary<EntityUid, DamageSpecifier> Progress = new();

    /// <summary>
    /// Damage accumulated from sources without an origin. Server only.
    /// </summary>
    [DataField]
    public DamageSpecifier UnknownProgress = new();
}
