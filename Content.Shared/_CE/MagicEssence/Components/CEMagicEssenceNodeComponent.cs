using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.MagicEssence.Components;

/// <summary>
/// A magic essence node holding 3 randomly rolled essence aspects, assigned on <see cref="Robust.Shared.GameObjects.MapInitEvent"/>.
/// Each field colors one of the node's sprite layers - see the client-side magic essence node system.
/// The node also passively generates essence reagent of one of its 3 aspects (weighted 70/20/10) into
/// its own solution every <see cref="GenerationInterval"/> - see the server-side magic essence node system.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class CEMagicEssenceNodeComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<CEMagicEssenceTypePrototype>? EssenceA;

    [DataField, AutoNetworkedField]
    public ProtoId<CEMagicEssenceTypePrototype>? EssenceB;

    [DataField, AutoNetworkedField]
    public ProtoId<CEMagicEssenceTypePrototype>? EssenceC;

    /// <summary>
    /// Sprite layer map key that <see cref="EssenceA"/> colors.
    /// </summary>
    [DataField]
    public string EssenceALayer = "essenceA";

    /// <summary>
    /// Sprite layer map key that <see cref="EssenceB"/> colors.
    /// </summary>
    [DataField]
    public string EssenceBLayer = "essenceB";

    /// <summary>
    /// Sprite layer map key that <see cref="EssenceC"/> colors.
    /// </summary>
    [DataField]
    public string EssenceCLayer = "essenceC";

    /// <summary>
    /// How often the node generates 1u of essence reagent, picked among its 3 rolled aspects
    /// (70% <see cref="EssenceA"/> / 20% <see cref="EssenceB"/> / 10% <see cref="EssenceC"/>).
    /// </summary>
    [DataField]
    public TimeSpan GenerationInterval = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Name of the solution that generated essence is added to.
    /// </summary>
    [DataField]
    public string SolutionName = "essence";

    [ViewVariables]
    public Entity<SolutionComponent>? Solution;

    /// <summary>
    /// Next time the node should generate essence.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextGenerationTime = TimeSpan.Zero;

    /// <summary>
    /// When the node was spawned, i.e. when <see cref="Lifetime"/> started counting down. Set once on
    /// <see cref="Robust.Shared.GameObjects.MapInitEvent"/> alongside a matching
    /// <see cref="Robust.Shared.Spawners.TimedDespawnComponent.Lifetime"/> - networked (unlike
    /// TimedDespawnComponent's own countdown) so the client can derive the fade in/out progress
    /// without guessing at server-only state.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan SpawnTime = TimeSpan.Zero;

    /// <summary>
    /// Total lifetime rolled for this node on spawn - randomized between <see cref="MinLifetime"/> and
    /// <see cref="MaxLifetime"/> server-side. Together with <see cref="SpawnTime"/>, drives the
    /// client-side fade in/out curve.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Lifetime = TimeSpan.Zero;

    /// <summary>
    /// Lower bound for the random <see cref="Lifetime"/> rolled on spawn.
    /// </summary>
    [DataField]
    public TimeSpan MinLifetime = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Upper bound for the random <see cref="Lifetime"/> rolled on spawn.
    /// </summary>
    [DataField]
    public TimeSpan MaxLifetime = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Random total scientific interest point budget rolled for this node on spawn, distributed
    /// across its 3 rolled aspects 70/20/10 - see
    /// <see cref="Content.Shared._CE.Science.Components.CEScientificInterestComponent"/> (added to
    /// the node alongside <see cref="EssenceA"/>/<see cref="EssenceB"/>/<see cref="EssenceC"/> by the
    /// server-side magic essence node system).
    /// </summary>
    [DataField]
    public MinMax InterestPoints = new(10, 20);

    /// <summary>
    /// When set, a <see cref="CEMagicEssenceNodeStabilizerComponent"/> anchored+powered on this
    /// node's tile is freezing its fade timing and despawn countdown as of this moment - the node
    /// keeps generating essence as normal, it just stops aging and can no longer expire. See the
    /// server-side magic essence node system's <c>StopNodeTime</c>/<c>ResumeNodeTime</c>. Null means
    /// the node ages normally. While set, its <see cref="Robust.Shared.Spawners.TimedDespawnComponent"/>
    /// is removed entirely (that system doesn't respect entity pause) - <see cref="SpawnTime"/> plus
    /// <see cref="Lifetime"/> minus the current time gives the remaining despawn countdown to
    /// restore it with on resume.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? StopTime;

    /// <summary>
    /// Client-only: cached 70/20/10 blend of <see cref="EssenceA"/>/<see cref="EssenceB"/>/<see cref="EssenceC"/>'s
    /// colors, re-derived whenever the rolled aspects change instead of every render frame.
    /// </summary>
    [ViewVariables]
    public Color? LightColor;
}
