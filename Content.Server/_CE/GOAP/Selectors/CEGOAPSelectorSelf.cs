using Content.Shared._CE.GOAP.Selectors;

namespace Content.Server._CE.GOAP.Selectors;

/// <summary>
/// Returns the agent itself. Used for self-targeted actions like self-heal.
/// </summary>
public sealed partial class CEGOAPSelectorSelf : CEGOAPTargetSelectorBase<CEGOAPSelectorSelf>
{
}

public sealed partial class CEGOAPSelectorSelfSystem : CEGOAPTargetSelectorSystem<CEGOAPSelectorSelf>
{
    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

    protected override void Resolve(ref CEGOAPSelectorResolveEvent<CEGOAPSelectorSelf> ev)
    {
        ev.Entity = ev.Agent;
        if (_xformQuery.TryGetComponent(ev.Agent, out var xform))
            ev.Position = xform.Coordinates;
    }
}
