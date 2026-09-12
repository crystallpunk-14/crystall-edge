using System.Numerics;
using Content.Shared._CE.GOAP.Selectors;
using Robust.Shared.Random;

namespace Content.Server._CE.GOAP.Selectors;

/// <summary>
/// Returns a random point in a radius around the agent. Only produces a position.
/// </summary>
public sealed partial class CEGOAPSelectorRandomPosition : CEGOAPTargetSelectorBase<CEGOAPSelectorRandomPosition>
{
    [DataField]
    public float Radius = 8f;
}

public sealed partial class CEGOAPSelectorRandomPositionSystem : CEGOAPTargetSelectorSystem<CEGOAPSelectorRandomPosition>
{
    [Dependency] private IRobustRandom _random = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

    protected override void Resolve(ref CEGOAPSelectorResolveEvent<CEGOAPSelectorRandomPosition> ev)
    {
        if (!_xformQuery.TryGetComponent(ev.Agent, out var xform))
            return;

        var angle = _random.NextFloat(0f, MathF.PI * 2f);
        var radius = _random.NextFloat(0f, ev.Selector.Radius);
        var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

        ev.Position = xform.Coordinates.Offset(offset);
    }
}
