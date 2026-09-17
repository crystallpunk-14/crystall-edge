using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Murk.Components;

/// <summary>
/// Marks the Lucson Sphere entity that <c>CEMurkConsumingRuleSystem</c> tracks and drains over the round.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEMurkLusconSphereComponent : Component
{
    [DataField, AutoNetworkedField]
    public CEMurkSphereState State = CEMurkSphereState.Stable;
}

[Serializable, NetSerializable]
public enum CEMurkSphereState : byte
{
    Stable,
    Cracked,
    Collapsing,
    Fixed,
}
