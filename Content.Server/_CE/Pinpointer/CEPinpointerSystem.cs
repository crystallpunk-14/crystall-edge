using System.Numerics;
using Content.Shared._CE.Pinpointer;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;

namespace Content.Server._CE.Pinpointer;

public sealed partial class CEPinpointerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CEPinpointerComponent>();
        while (query.MoveNext(out var uid, out var pinpointer))
        {
            if (now < pinpointer.NextUpdate)
                continue;

            pinpointer.NextUpdate = now + pinpointer.UpdateInterval;

            if (now >= pinpointer.NextSearch)
            {
                pinpointer.NextSearch = now + pinpointer.SearchInterval;
                pinpointer.Target = FindTarget((uid, pinpointer));
            }

            UpdateDirection((uid, pinpointer));
        }
    }

    private void UpdateDirection(Entity<CEPinpointerComponent> ent)
    {
        if (ent.Comp.Target is not { } target ||
            TerminatingOrDeleted(target) ||
            !TryGetPlanarDirection(ent, target, out var dir) ||
            dir.Length() <= ent.Comp.ReachedDistance)
        {
            SetHasDirection(ent, false);
            return;
        }

        SetHasDirection(ent, true);

        var angle = dir.ToWorldAngle();
        if (ent.Comp.ArrowAngle.EqualsApprox(angle, ent.Comp.Precision))
            return;

        ent.Comp.ArrowAngle = angle;
        Dirty(ent);
    }

    private void SetHasDirection(Entity<CEPinpointerComponent> ent, bool value)
    {
        if (ent.Comp.HasDirection == value)
            return;

        ent.Comp.HasDirection = value;
        Dirty(ent);
    }

    private EntityUid? FindTarget(Entity<CEPinpointerComponent> ent)
    {
        var components = ent.Comp.Whitelist.Components;
        if (components == null || components.Length == 0)
        {
            Log.Error($"{ToPrettyString(ent)} pinpointer whitelist has no components to search targets by");
            return null;
        }

        var reg = EntityManager.ComponentFactory.GetRegistration(components[0]);

        EntityUid? closest = null;
        var closestDist = float.MaxValue;
        foreach (var (candidate, _) in EntityManager.GetAllComponents(reg.Type))
        {
            if (!_whitelist.IsValid(ent.Comp.Whitelist, candidate))
                continue;

            if (!TryGetPlanarDirection(ent, candidate, out var dir))
                continue;

            var dist = dir.LengthSquared();
            if (dist >= closestDist)
                continue;

            closestDist = dist;
            closest = candidate;
        }

        return closest;
    }

    /// <summary>
    /// World direction from <paramref name="from"/> to <paramref name="to"/> if they share a z-network (or a map),
    /// ignoring the z-level difference.
    /// </summary>
    private bool TryGetPlanarDirection(EntityUid from, EntityUid to, out Vector2 dir)
    {
        dir = Vector2.Zero;

        if (Transform(from).MapUid is not { } fromMap || Transform(to).MapUid is not { } toMap)
            return false;

        if (!_zLevels.TryGetZLevelOffset(fromMap, toMap, out _))
            return false;

        dir = _transform.GetWorldPosition(to) - _transform.GetWorldPosition(from);
        return true;
    }
}
