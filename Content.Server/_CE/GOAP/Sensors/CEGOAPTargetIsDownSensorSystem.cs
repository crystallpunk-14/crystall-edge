using Content.Shared._CE.GOAP.Selectors;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server._CE.GOAP.Sensors;

[DataDefinition]
public sealed partial class CEGOAPTargetIsDownSensorEntry
    : CEGOAPSensorEntry<CEGOAPTargetIsDownSensorEntry, CEGOAPTargetIsDownSensorComponent>;

/// <summary>
/// Checks if the target slot resolves to an incapacitated (critical or dead) entity.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPTargetIsDownSensorComponent : CEGOAPSensorComponent<CEGOAPTargetIsDownSensorEntry>;

public sealed partial class CEGOAPTargetIsDownSensorSystem
    : CEGOAPSensorSystem<CEGOAPTargetIsDownSensorComponent, CEGOAPTargetIsDownSensorEntry>
{
    // CrystallEdge: Rogue used CEMobStateSystem (CE-only). This fork has no CE health stack,
    // so use vanilla MobStateSystem instead.
    [Dependency] private MobStateSystem _mobState = default!;

    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery = default!;

    protected override bool Evaluate(EntityUid agent, CEGOAPTargetIsDownSensorEntry entry, CEGOAPSelectorResult target)
    {
        return target.Entity is { } entity && (
            _mobStateQuery.TryGetComponent(entity, out var mobState)
                ? _mobState.IsIncapacitated(entity, mobState)
                : Terminating(entity));
    }
}
