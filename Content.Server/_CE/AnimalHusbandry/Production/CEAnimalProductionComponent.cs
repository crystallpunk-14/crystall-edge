using Content.Shared.EntityConditions;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._CE.AnimalHusbandry.Production;

/// <summary>Schedules one pending animal product, stored in the granted action's LimitedCharges.</summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CEAnimalProductionComponent : Component
{
    [DataField(required: true)] public EntProtoId OutputAction;
    [DataField(required: true)] public EntProtoId ProductPrototype;
    [DataField(required: true)] public EntProtoId FertilizedPrototype;
    [DataField(required: true)] public EntityWhitelist PopulationWhitelist = default!;
    [DataField(required: true)] public int PopulationLimit;
    [DataField, AlwaysPushInheritance] public EntityCondition[] Conditions = [];
    [DataField(required: true)] public float HungerCost;
    [DataField(required: true)] public TimeSpan FirstMinimum;
    [DataField(required: true)] public TimeSpan FirstMaximum;
    [DataField(required: true)] public TimeSpan RepeatMinimum;
    [DataField(required: true)] public TimeSpan RepeatMaximum;
    [DataField] public TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextProductionAt;
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPollAt;
    [DataField] public bool WaitingForOutputSpend;
    [DataField] public bool Disabled;
}
