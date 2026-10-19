using Content.Shared._CE.GOAP.Selectors;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.GOAP.Sensors;

public sealed partial class CEGOAPHasComponentSensorEntry
    : CEGOAPSensorEntry<CEGOAPHasComponentSensorEntry, CEGOAPHasComponentSensorComponent>
{
    /// <summary>
    /// Components the target slot's entity must all have.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();
}

/// <summary>
/// Writes true when the target slot's entity has every listed component.
/// Re-evaluated as soon as a component is added to or removed from the agent itself.
/// </summary>
[RegisterComponent]
public sealed partial class CEGOAPHasComponentSensorComponent : CEGOAPSensorComponent<CEGOAPHasComponentSensorEntry>;

public sealed partial class CEGOAPHasComponentSensorSystem
    : CEGOAPSensorSystem<CEGOAPHasComponentSensorComponent, CEGOAPHasComponentSensorEntry>
{
    [Dependency] private EntityQuery<CEGOAPHasComponentSensorComponent> _sensorQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        EntityManager.ComponentAdded += OnComponentAdded;
        EntityManager.ComponentRemoved += OnComponentRemoved;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        EntityManager.ComponentAdded -= OnComponentAdded;
        EntityManager.ComponentRemoved -= OnComponentRemoved;
    }

    private void OnComponentAdded(AddedComponentEventArgs args)
    {
        OnComponentChanged(args.BaseArgs.Owner);
    }

    private void OnComponentRemoved(RemovedComponentEventArgs args)
    {
        OnComponentChanged(args.BaseArgs.Owner);
    }

    private void OnComponentChanged(EntityUid uid)
    {
        if (!_sensorQuery.TryComp(uid, out var sensor) || TerminatingOrDeleted(uid))
            return;

        EvaluateAll((uid, sensor));
    }

    protected override bool Evaluate(EntityUid agent, CEGOAPHasComponentSensorEntry entry, CEGOAPSelectorResult target)
    {
        if (target.Entity is not { } entity)
            return false;

        foreach (var registration in entry.Components.Values)
        {
            if (!HasComp(entity, registration.Component.GetType()))
                return false;
        }

        return true;
    }
}
