using Content.Shared._CE.GOAP.Sensors;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.GOAP.Prototypes;

/// <summary>
/// A reusable package of GOAP behavior: goals, the actions that satisfy them and the sensors that
/// produce the keys they depend on. Mobs list packages in <see cref="Components.CEGOAPComponent.Behaviors"/>;
/// they are expanded onto the mob on MapInit, in addition to the mob's own inline goals and actions.
/// </summary>
/// <remarks>
/// Goal, action and sensor instances are shared by every mob using the package, so they must stay
/// pure data. Per-agent runtime state belongs in components.
/// </remarks>
[Prototype("GOAPBehavior")]
public sealed partial class CEGOAPBehaviorPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Other packages pulled in by this one. Each package is applied at most once per mob.
    /// </summary>
    [DataField]
    public List<ProtoId<CEGOAPBehaviorPrototype>> Includes = new();

    [DataField(serverOnly: true)]
    public List<CEGOAPGoal> Goals = new();

    [DataField(serverOnly: true)]
    public List<CEGOAPAction> Actions = new();

    [DataField(serverOnly: true)]
    public List<CEGOAPSensorEntryBase> Sensors = new();
}
