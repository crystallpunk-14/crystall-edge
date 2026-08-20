using Robust.Shared.GameStates;

namespace Content.Shared._CE.Animation.Core.Components;

/// <summary>
/// Baseline sprite rotation (in degrees) applied to an entity's animated sprite clone
/// before any rotation keyframes from the playing animation are applied.
/// Lets an item's held sprite be authored in a different orientation than the animation
/// system's zero-angle convention, without coupling the generic animation engine to
/// any specific consumer (weapons, tools, etc).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CERotationForAnimationComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Rotation;
}
