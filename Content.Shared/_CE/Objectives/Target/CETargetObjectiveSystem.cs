using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Target.Components;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Shared._CE.Objectives.Target;

/// <summary>
/// Handles picking, storing and clearing the target of <see cref="CETargetObjectiveComponent"/>
/// objectives. Candidates are gathered via <see cref="CEGetObjectiveTargetCandidatesEvent"/> (e.g. by
/// <see cref="CETargetPlayersObjectiveComponent"/>) and filtered via
/// <see cref="CEValidateObjectiveTargetCandidateEvent"/> (e.g. by a condition requiring the candidate
/// to have their own personal objectives).
/// </summary>
public sealed partial class CETargetObjectiveSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedJobSystem _job = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private CESharedObjectiveSystem _objective = default!;

    [SubscribeLocalEvent]
    private void OnInitializeObjective(Entity<CETargetObjectiveComponent> ent, ref CEInitializeObjectiveEvent args)
    {
        if (!TryGetCandidate(args.Holder, ent, out var candidate))
            return;

        SetTarget(ent.AsNullable(), candidate);
    }

    [SubscribeLocalEvent]
    private void OnTargetShutdown(Entity<CETargetObjectiveMarkerComponent> ent, ref ComponentShutdown args)
    {
        foreach (var objective in ent.Comp.Objectives)
        {
            if (TryComp<CETargetObjectiveComponent>(objective, out var comp))
                SetTarget((objective, comp), null);
        }
    }

    [SubscribeLocalEvent]
    private void OnObjectiveTerminating(Entity<CETargetObjectiveComponent> ent, ref EntityTerminatingEvent args)
    {
        SetTarget(ent.AsNullable(), null);
    }

    // Retries unresolved targets on any objective list change, since a candidate may be rejected
    // just because their own objectives don't exist yet. Broadcast because the directed slot for
    // CEObjectiveHolderComponent+CEObjectivesChangedEvent is already taken by CEShareTargetObjectiveSystem.
    [SubscribeLocalEvent]
    private void OnObjectivesChanged(ref CEObjectivesChangedEvent args)
    {
        var query = EntityQueryEnumerator<CEObjectiveHolderComponent>();
        while (query.MoveNext(out var holderUid, out var holderComp))
        {
            foreach (var objective in holderComp.Objectives)
            {
                if (!TryComp<CETargetObjectiveComponent>(objective, out var targetComp) || targetComp.Target != null)
                    continue;

                if (TryGetCandidate((holderUid, holderComp), (objective, targetComp), out var candidate))
                    SetTarget((objective, targetComp), candidate);
            }
        }
    }

    /// <summary>
    /// Picks a random valid target candidate for an objective from its holder's other objectives'
    /// perspective (excluding whatever they're already targeting).
    /// </summary>
    public bool TryGetCandidate(
        Entity<CEObjectiveHolderComponent> holder,
        Entity<CETargetObjectiveComponent> ent,
        [NotNullWhen(true)] out EntityUid? candidate)
    {
        candidate = null;
        var candidates = GetTargetCandidates(holder, ent).ToList();
        if (candidates.Count == 0)
            return false;

        candidate = _random.Pick(candidates);
        return true;
    }

    public IEnumerable<EntityUid> GetTargetCandidates(Entity<CEObjectiveHolderComponent> holder, Entity<CETargetObjectiveComponent> ent)
    {
        var otherTargets = new HashSet<EntityUid>();
        foreach (var objective in _objective.GetObjectives(holder.AsNullable()))
        {
            if (TryComp<CETargetObjectiveComponent>(objective.Owner, out var targetComp) && targetComp.Target is { } target)
                otherTargets.Add(target);
        }

        var ev = new CEGetObjectiveTargetCandidatesEvent(holder, []);
        RaiseLocalEvent(ent.Owner, ref ev);

        foreach (var candidate in ev.Candidates)
        {
            // Don't share targets between multiple targeted objectives of the same holder.
            if (otherTargets.Contains(candidate))
                continue;

            var checkEv = new CEValidateObjectiveTargetCandidateEvent(holder, candidate);
            RaiseLocalEvent(ent.Owner, ref checkEv);

            if (checkEv.Valid)
                yield return candidate;
        }
    }

    public bool TryGetTarget(Entity<CETargetObjectiveComponent?> ent, [NotNullWhen(true)] out EntityUid? target)
    {
        target = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        target = ent.Comp.Target;
        return target != null;
    }

    public EntityUid? GetTargetOrNull(Entity<CETargetObjectiveComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp) ? ent.Comp.Target : null;
    }

    /// <summary>
    /// Sets the target for a given <see cref="CETargetObjectiveComponent"/>.
    /// </summary>
    public void SetTarget(Entity<CETargetObjectiveComponent?> ent, EntityUid? target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var oldTarget = ent.Comp.Target;
        if (oldTarget is { } old && TryComp<CETargetObjectiveMarkerComponent>(old, out var oldMarker))
            oldMarker.Objectives.Remove(ent);

        ent.Comp.Target = target;

        if (target is { } newTarget)
        {
            if (ent.Comp.Title != null)
            {
                var name = Name(newTarget);
                var job = string.Empty;
                if (_mind.TryGetMind(newTarget, out var mind, out _))
                    _job.MindTryGetJobName(mind, out job);

                var title = Loc.GetString(ent.Comp.Title, ("targetName", name), ("job", job));
                _metaData.SetEntityName(ent, title);
            }

            var marker = EnsureComp<CETargetObjectiveMarkerComponent>(newTarget);
            marker.Objectives.Add(ent);
        }

        var ev = new CEObjectiveTargetChangedEvent(oldTarget, target);
        RaiseLocalEvent(ent.Owner, ref ev, true);

        _objective.RefreshObjectiveProgress(ent.Owner);
    }

    /// <summary>
    /// Returns the objectives that are targeting a given entity.
    /// </summary>
    [PublicAPI]
    public IEnumerable<EntityUid> GetTargetingObjectives(Entity<CETargetObjectiveMarkerComponent?> ent)
    {
        return GetTargetingObjectives<CEObjectiveComponent>(ent).Select(e => e.Owner);
    }

    /// <summary>
    /// Returns the objectives that are targeting a given entity, filtered by a particular component.
    /// </summary>
    public IEnumerable<Entity<TComponent>> GetTargetingObjectives<TComponent>(Entity<CETargetObjectiveMarkerComponent?> ent)
        where TComponent : Component
    {
        if (!Resolve(ent, ref ent.Comp, false))
            yield break;

        foreach (var objective in ent.Comp.Objectives)
        {
            if (TryComp<TComponent>(objective, out var comp))
                yield return (objective, comp);
        }
    }
}
