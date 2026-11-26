using System.Numerics;
using Content.Shared._CE.Actions.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.Shared._CE.Actions;

/// <summary>
/// Resolves where a targeted action actually lands. Casting and targeting visuals must both go through here.
/// </summary>
public sealed partial class CEActionAimSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private OccluderSystem _occluder = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<CEActionRequireLineOfSightComponent> _lineOfSightQuery = default!;
    [Dependency] private EntityQuery<OccluderComponent> _occluderQuery = default!;
    [Dependency] private EntityQuery<TargetActionComponent> _targetQuery = default!;
    [Dependency] private EntityQuery<WorldTargetActionComponent> _worldTargetQuery = default!;

    // Keeps a point clamped to the range edge from failing the range check on float error.
    private const float RangeMargin = 0.01f;

    // Gap left between the aim point and whatever cut it short.
    private const float ObstacleGap = 0.5f;

    // The aim point never collapses into the user, so the cast keeps its direction.
    private const float MinDistance = 0.01f;

    private const int SearchSteps = 10;

    private readonly List<RayCastResults> _rayResults = new();

    /// <summary>
    /// Replaces the coordinates of a world-target action request with the resolved aim point.
    /// </summary>
    public RequestPerformActionEvent ClampRequest(EntityUid user, EntityUid action, RequestPerformActionEvent ev)
    {
        if (ev.EntityCoordinatesTarget is not { } netCoords || !_worldTargetQuery.HasComp(action))
            return ev;

        var target = GetCoordinates(netCoords);
        if (!target.IsValid(EntityManager))
            return ev;

        var aim = GetAimPoint(user, target, action);
        if (aim == target)
            return ev;

        return new RequestPerformActionEvent(ev.Action, ev.EntityTarget, GetNetCoordinates(aim));
    }

    /// <summary>
    /// Pulls a clicked point back along the ray from the user, so that it fits the action range and stops short of
    /// obstacles the action cannot pass. Returns <paramref name="target"/> unchanged if it is already reachable.
    /// </summary>
    public EntityCoordinates GetAimPoint(EntityUid user, EntityCoordinates target, EntityUid action)
    {
        if (!_targetQuery.TryComp(action, out var targetAction))
            return target;

        var origin = _transform.GetMapCoordinates(user);
        var targetMap = _transform.ToMapCoordinates(target);
        if (origin.MapId != targetMap.MapId)
            return target;

        var offset = targetMap.Position - origin.Position;
        var length = offset.Length();
        if (length <= MinDistance)
            return target;

        var direction = offset / length;
        var distance = length;

        if (targetAction.Range > 0f && distance > targetAction.Range)
            distance = targetAction.Range - RangeMargin;

        var needsSight = _lineOfSightQuery.HasComp(action);
        var needsAccess = targetAction.CheckCanAccess;

        if (IsBlocked(user, origin, direction, distance, needsSight, needsAccess))
        {
            var clear = 0f;
            var blocked = distance;
            for (var i = 0; i < SearchSteps; i++)
            {
                var middle = (clear + blocked) / 2f;
                if (IsBlocked(user, origin, direction, middle, needsSight, needsAccess))
                    blocked = middle;
                else
                    clear = middle;
            }

            distance = MathF.Max(blocked - ObstacleGap, MinDistance);
        }

        if (distance >= length)
            return target;

        var aim = new MapCoordinates(origin.Position + direction * distance, origin.MapId);
        return _transform.ToCoordinates(target.EntityId, aim);
    }

    /// <summary>
    /// False only if the action requires line of sight and the user has none to the target.
    /// </summary>
    public bool PassesLineOfSight(EntityUid user, EntityCoordinates target, EntityUid action)
    {
        if (!_lineOfSightQuery.HasComp(action))
            return true;

        var range = _targetQuery.TryComp(action, out var targetAction) ? targetAction.Range : 0f;
        return _examine.InRangeUnOccluded(user, target, range);
    }

    private bool IsBlocked(
        EntityUid user,
        MapCoordinates origin,
        Vector2 direction,
        float distance,
        bool needsSight,
        bool needsAccess)
    {
        var end = new MapCoordinates(origin.Position + direction * distance, origin.MapId);

        if (needsAccess && !_interaction.InRangeUnobstructed(user, end, range: 0f))
            return true;

        return needsSight && IsOccluded(origin, direction, distance);
    }

    // Unlike the examine check, an occluder containing the end point still blocks: the aim must stop before the wall.
    private bool IsOccluded(MapCoordinates origin, Vector2 direction, float distance)
    {
        _occluder.IntersectRay(_rayResults, origin.MapId, new Ray(origin.Position, direction), distance);

        foreach (var result in _rayResults)
        {
            if (_occluderQuery.TryComp(result.HitEntity, out var occluder) &&
                _occluder.ContainsPoint(occluder, Transform(result.HitEntity), origin.Position))
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
