using Content.Shared._CE.GOAP.Selectors;

namespace Content.Server._CE.GOAP.Sensors;

[DataDefinition]
public sealed partial class CEGOAPHasTargetSensorEntry
    : CEGOAPSensorEntry<CEGOAPHasTargetSensorEntry, CEGOAPHasTargetSensorComponent>;

/// <summary>
/// Writes true when the entry's target slot resolves to an entity. Re-evaluated whenever the
/// agent's knowledge changes.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPHasTargetSensorComponent : CEGOAPSensorComponent<CEGOAPHasTargetSensorEntry>;

public sealed partial class CEGOAPHasTargetSensorSystem
    : CEGOAPSensorSystem<CEGOAPHasTargetSensorComponent, CEGOAPHasTargetSensorEntry>
{
    protected override bool Evaluate(EntityUid agent, CEGOAPHasTargetSensorEntry entry, CEGOAPSelectorResult target)
    {
        return target.Entity != null;
    }
}
