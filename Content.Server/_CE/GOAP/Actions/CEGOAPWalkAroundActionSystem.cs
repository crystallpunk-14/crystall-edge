using Content.Server._CE.GOAP.Steering;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared.Examine;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CE.GOAP.Actions;

/// <summary>
/// Walks to a random walkable tile near the <see cref="CEGOAPAction.Target"/> slot and idles there a bit.
/// A tile qualifies when it is within <see cref="Radius"/> of the anchor, or when the anchor
/// can be seen from it within <see cref="LineOfSightRadius"/>.
/// </summary>
public sealed partial class CEGOAPWalkAroundAction : CEGOAPActionBase<CEGOAPWalkAroundAction>
{
    [DataField]
    public float Radius = 4f;

    /// <summary>
    /// Farthest distance at which a tile still qualifies by seeing the anchor. Null disables it.
    /// </summary>
    [DataField]
    public float? LineOfSightRadius;

    /// <summary>
    /// Number of random tiles tried when looking for a destination.
    /// </summary>
    [DataField]
    public int Samples = 16;

    [DataField]
    public TimeSpan MinIdleTime = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan MaxIdleTime = TimeSpan.FromSeconds(3);
}

/// <summary>
/// Per-agent state of a running <see cref="CEGOAPWalkAroundAction"/>.
/// </summary>
[RegisterComponent, Access(typeof(CEGOAPWalkAroundActionSystem))]
public sealed partial class CEGOAPWalkAroundComponent : Component
{
    /// <summary>
    /// When the agent stops idling at the destination. Null while still walking.
    /// </summary>
    [ViewVariables]
    public TimeSpan? IdleUntil;
}

public sealed partial class CEGOAPWalkAroundActionSystem : CEGOAPActionSystem<CEGOAPWalkAroundAction>
{
    [Dependency] private CEGOAPSteeringSystem _steering = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    protected override void OnActionStartup(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionStartupEvent<CEGOAPWalkAroundAction> args)
    {
        var state = EnsureComp<CEGOAPWalkAroundComponent>(ent);
        state.IdleUntil = null;

        if (TryResolveCoords(ent, args.Action.Target, out var anchor) &&
            PickDestination(_transform.ToMapCoordinates(anchor), args.Action) is { } destination)
        {
            _steering.Navigate(ent, _transform.ToCoordinates(destination), 1.5f);
            return;
        }

        // Nowhere to go: just stand for a while.
        StartIdle(state, args.Action);
    }

    protected override void OnActionUpdate(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionUpdateEvent<CEGOAPWalkAroundAction> args)
    {
        if (!TryComp<CEGOAPWalkAroundComponent>(ent, out var state))
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        if (state.IdleUntil is { } idleUntil)
        {
            args.Status = _timing.CurTime >= idleUntil ? CEGOAPActionStatus.Finished : CEGOAPActionStatus.Running;
            return;
        }

        switch (_steering.Continue(ent))
        {
            case CEGOAPSteeringStatus.InRange:
                StartIdle(state, args.Action);
                args.Status = CEGOAPActionStatus.Running;
                return;
            case CEGOAPSteeringStatus.NoPath:
                args.Status = CEGOAPActionStatus.Failed;
                return;
            default:
                args.Status = CEGOAPActionStatus.Running;
                return;
        }
    }

    protected override void OnActionShutdown(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionShutdownEvent<CEGOAPWalkAroundAction> args)
    {
        RemComp<CEGOAPWalkAroundComponent>(ent);
        _steering.Stop(ent.Owner);
    }

    private void StartIdle(CEGOAPWalkAroundComponent state, CEGOAPWalkAroundAction action)
    {
        state.IdleUntil = _timing.CurTime + _random.Next(action.MinIdleTime, action.MaxIdleTime);
    }

    private MapCoordinates? PickDestination(MapCoordinates anchor, CEGOAPWalkAroundAction action)
    {
        var maxDistance = Math.Max(action.Radius, action.LineOfSightRadius ?? 0f);

        for (var i = 0; i < action.Samples; i++)
        {
            var offset = _random.NextAngle().ToVec() * _random.NextFloat(0f, maxDistance);
            var candidate = new MapCoordinates(anchor.Position + offset, anchor.MapId);

            if (!IsWalkable(candidate))
                continue;

            if (offset.Length() <= action.Radius)
                return candidate;

            if (action.LineOfSightRadius is { } losRadius &&
                _examine.InRangeUnOccluded(candidate, anchor, losRadius, null))
            {
                return candidate;
            }
        }

        return null;
    }

    private bool IsWalkable(MapCoordinates coords)
    {
        if (!_mapSystem.TryFindGridAt(coords, out var gridUid, out var grid))
            return false;

        var tile = _mapSystem.WorldToTile(gridUid, grid, coords.Position);
        if (!_mapSystem.TryGetTileRef(gridUid, grid, tile, out var tileRef) || tileRef.Tile.IsEmpty)
            return false;

        return _mapSystem.AnchoredEntityCount(gridUid, grid, tile) == 0;
    }
}
