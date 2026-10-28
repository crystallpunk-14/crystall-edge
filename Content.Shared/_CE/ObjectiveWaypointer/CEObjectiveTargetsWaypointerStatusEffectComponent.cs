using Content.Shared._CE.Waypointer;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.ObjectiveWaypointer;

/// <summary>
/// Status effect that points a waypointer at the target of every targeted objective held by the
/// affected entity's mind.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(CEObjectiveWaypointerSystem))]
public sealed partial class CEObjectiveTargetsWaypointerStatusEffectComponent : Component
{
    [DataField(required: true)]
    public ProtoId<CEWaypointerPrototype> Waypointer;
}
