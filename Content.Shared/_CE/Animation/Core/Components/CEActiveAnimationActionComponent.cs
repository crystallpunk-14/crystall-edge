using Content.Shared._CE.Animation.Core.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CE.Animation.Core.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEActiveAnimationActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<CEEntityEffectAnimationPrototype>? ActiveAnimation;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan? StartAnimationTime;

    [DataField, AutoNetworkedField]
    public float AnimationSpeed = 1f;

    /// <summary>
    /// If true, it fixes the caster's rotation towards TargetEntity or TargetCoordinates,
    /// if they are not null. If they are null, it simply does not allow rotation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool LockRotation;

    /// <summary>
    /// The entity targeted by the current action
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? TargetEntity;

    /// <summary>
    /// Coordinates to which the current action is directed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityCoordinates? TargetCoordinates;

    [DataField, AutoNetworkedField]
    public EntityUid? Used;

    [AutoNetworkedField]
    public TimeSpan LastEvent = TimeSpan.FromSeconds(-1);
}
