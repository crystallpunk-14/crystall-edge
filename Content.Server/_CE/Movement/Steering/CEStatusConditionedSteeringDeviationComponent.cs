using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._CE.Movement.Steering;

/// <summary>
/// Prototype-authored intermittent steering deviation while a specific status effect is active.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(CEStatusConditionedSteeringDeviationSystem))]
public sealed partial class CEStatusConditionedSteeringDeviationComponent : Component
{
    [DataField(required: true)]
    public EntProtoId RequiredStatusEffect;

    [DataField]
    public TimeSpan MinDeviationInterval = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan MaxDeviationInterval = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan MinDeviationDuration = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan MaxDeviationDuration = TimeSpan.FromSeconds(1.25);

    /// <summary>
    /// Portion of existing context-steering interest retained during a deviation.
    /// </summary>
    [DataField]
    public float RetainedInterest = 0.25f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDeviation;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan DeviationEnd;

    [DataField]
    public Vector2 DeviationDirection;
}
