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

/// <summary>
/// Raised on a <see cref="CEMurkLusconSphereComponent"/> entity whenever its
/// <see cref="CEMurkLusconSphereComponent.State"/> changes.
/// </summary>
public sealed class CEMurkSphereStateChangedEvent(CEMurkSphereState oldState, CEMurkSphereState newState) : EntityEventArgs
{
    public readonly CEMurkSphereState OldState = oldState;
    public readonly CEMurkSphereState NewState = newState;
}
