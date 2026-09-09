using Content.Shared.EntityConditions;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._CE.AnimalHusbandry.Production;

/// <summary>Bounded residence at the nest after laying; separate from its egg slots.</summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEAnimalRoostComponent : Component
{
    [DataField(required: true)] public TimeSpan MinimumDuration;
    [DataField(required: true)] public TimeSpan MaximumDuration;
    [DataField] public float HostRange = 0.9f;
    [DataField] public EntityCondition[] Conditions = [];
    [DataField] public EntityUid? Host;
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan Until;
}
