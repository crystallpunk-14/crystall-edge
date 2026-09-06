using System.Numerics;
using Content.Server._CE.GOAP.Navigation;
using Content.Shared._CE.GOAP.Selectors;
using Content.Shared.Whitelist;

namespace Content.Server._CE.GOAP.Selectors;

/// <summary>
/// Selects the nearest in-range entity matching prototype-authored whitelist rules.
/// </summary>
[DataDefinition]
public sealed partial class CEGOAPSelectorNearestEntity
    : CEGOAPTargetSelectorBase<CEGOAPSelectorNearestEntity>, ICEGOAPTargetBackoffSelector
{
    [DataField(required: true)]
    public float Range;

    [DataField(required: true)]
    public EntityWhitelist Whitelist = default!;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public bool IncludeSelf;
}

public sealed partial class CEGOAPSelectorNearestEntitySystem
    : CEGOAPTargetSelectorSystem<CEGOAPSelectorNearestEntity>
{
    [Dependency] private CEGOAPTargetBackoffSystem _backoff = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    protected override void Resolve(ref CEGOAPSelectorResolveEvent<CEGOAPSelectorNearestEntity> ev)
    {
        if (!float.IsFinite(ev.Selector.Range) || ev.Selector.Range < 0f ||
            !TryComp(ev.Agent, out TransformComponent? origin))
            return;

        var originPosition = _transform.GetWorldPosition(origin);
        var bestDistance = float.MaxValue;
        EntityUid? best = null;
        _backoff.Prune(ev.Agent);

        foreach (var candidate in _lookup.GetEntitiesInRange(
                     origin.Coordinates,
                     ev.Selector.Range,
                     LookupFlags.Uncontained))
        {
            if (!ev.Selector.IncludeSelf && candidate == ev.Agent ||
                !_whitelist.CheckBoth(candidate, ev.Selector.Blacklist, ev.Selector.Whitelist) ||
                _backoff.IsRejected(ev.Agent, candidate) ||
                !TryComp(candidate, out TransformComponent? candidateTransform) ||
                candidateTransform.MapID != origin.MapID)
                continue;

            var distance = Vector2.DistanceSquared(
                originPosition,
                _transform.GetWorldPosition(candidateTransform));
            if (!float.IsFinite(distance) || distance > bestDistance ||
                distance.Equals(bestDistance) && best is { } current && candidate.Id > current.Id)
                continue;

            best = candidate;
            bestDistance = distance;
        }

        if (best is not { } result || !TryComp(result, out TransformComponent? resultTransform))
            return;

        ev.Entity = result;
        ev.Position = resultTransform.Coordinates;
    }
}
