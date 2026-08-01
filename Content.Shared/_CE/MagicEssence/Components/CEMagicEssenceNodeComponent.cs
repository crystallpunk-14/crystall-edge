using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared.Chemistry.Components;
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
}
