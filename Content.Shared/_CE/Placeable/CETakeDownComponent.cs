using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Placeable;

/// <summary>
/// Placed entity that can be taken down by hand, turning back into its item.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CETakeDownSystem))]
public sealed partial class CETakeDownComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Item;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);
}
