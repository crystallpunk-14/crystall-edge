using Content.Shared._CE.MagicEssence.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.MagicEssence.Components;

/// <summary>
/// It absorbs magical essence upon contact and disappears when it is full.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CEMagicEssenceHungryNodeComponent : Component
{
    /// <summary>
    /// Total hunger "volume" rolled across up to 3 essence types (70/20/10) on spawn.
    /// </summary>
    [DataField]
    public int HungerVolume = 10;

    /// <summary>
    /// Remaining amount of each essence type needed to satisfy this node's hunger. Entries reach 0
    /// (and get removed) as matching essence is fed to the node; once empty, the node is satisfied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> RequiredEssence = new();

    /// <summary>
    /// Copy of <see cref="RequiredEssence"/> as rolled on spawn, kept around (unmodified as the node
    /// is fed) so <see cref="Reward"/> can be scaled off the original hunger.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> InitialRequiredEssence = new();

    /// <summary>
    /// Spawned at the node's position once its hunger is satisfied (e.g. a shockwave, a mana-consume
    /// mishap effect).
    /// </summary>
    [DataField]
    public List<EntProtoId> VFX = new();

    /// <summary>
    /// Spawned at the node's position once its hunger is satisfied, granted a
    /// <see cref="Content.Shared._CE.Science.Components.CEScientificInterestComponent"/> worth double
    /// <see cref="InitialRequiredEssence"/>.
    /// </summary>
    [DataField]
    public EntProtoId? Reward;

    /// <summary>
    /// Set the instant <see cref="RequiredEssence"/> is drained to nothing, so a second orb colliding
    /// later in the same tick (before the node's queued deletion actually happens) can't trigger the
    /// satisfaction reward a second time.
    /// </summary>
    public bool Satisfied;

    /// <summary>
    /// Played whenever the node consumes a floating essence orb, matching or not - same sound as
    /// <see cref="Content.Server._CE.MagicEssence.Components.CEMagicEssenceAttractorComponent.ConsumeSound"/>.
    /// </summary>
    [DataField]
    public SoundSpecifier ConsumeSound = new SoundPathSpecifier("/Audio/_CE/Effects/essence_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithVariation(0.2f),
    };
}
