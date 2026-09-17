using Content.Shared._CE.Objectives;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Objectives.Components;

/// <summary>
/// Badge data shown on a CE objective's card in the character menu: source name, color, and
/// (optionally) a tooltip explaining it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(CESharedObjectiveSystem))]
public sealed partial class CEObjectiveDescriptorComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public LocId Name;

    [DataField, AutoNetworkedField]
    public Color Color = Color.White;

    [DataField, AutoNetworkedField]
    public LocId? Tooltip;
}
