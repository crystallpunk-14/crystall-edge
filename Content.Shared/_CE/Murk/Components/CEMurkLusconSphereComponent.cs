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
    public CEMurkSphereState State = CEMurkSphereState.PreRound;
}

[Serializable, NetSerializable]
public enum CEMurkSphereState : byte
{
    /// <summary>
    /// Before the crack: the sphere is whole and the city prepares.
    /// </summary>
    PreRound,

    /// <summary>
    /// The sphere has cracked and the collapse countdown runs.
    /// </summary>
    InGame,

    /// <summary>
    /// The Restoration Ritual is in progress: the core is exposed and vulnerable.
    /// </summary>
    Ritual,

    /// <summary>
    /// The ritual succeeded and the sphere is restored.
    /// </summary>
    Success,

    /// <summary>
    /// The countdown ran out or the core was destroyed: the murk consumes the city.
    /// </summary>
    Failure,
}

[Serializable, NetSerializable]
public enum CEMurkSphereVisuals : byte
{
    State,
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
