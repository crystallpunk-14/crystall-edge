using System.Linq;
using Content.Server._CE.Objectives.Target.Components;
using Content.Server._CE.Objectives;
using Content.Shared._CE.Objectives;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Target;
using Content.Shared._CE.Objectives.Target.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Utility;

namespace Content.Server._CE.Objectives.Target;

/// <summary>
/// Handles <see cref="CETargetCompleteOwnedObjectiveComponent"/>.
/// </summary>
public sealed partial class CETargetCompleteObjectivesSystem : EntitySystem
{
    [Dependency] private CEObjectiveSystem _objectives = default!;
    [Dependency] private CETargetObjectiveSystem _target = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;

    // Refreshing every CETargetCompleteOwnedObjectiveComponent whenever any objective's progress
    // changes could re-enter this same condition's own progress change - guard against that.
    private bool _loop;

    [SubscribeLocalEvent]
    private void OnTargetChanged(Entity<CETargetCompleteOwnedObjectiveComponent> ent, ref CEObjectiveTargetChangedEvent args)
    {
        if (args.OldTarget is { } oldTarget &&
            !TerminatingOrDeleted(oldTarget) &&
            !_target.GetTargetingObjectives<CETargetCompleteOwnedObjectiveComponent>(oldTarget).Any())
        {
            RemComp<CETargetCompleteOwnedObjectiveMarkerComponent>(oldTarget);
        }

        if (args.NewTarget is { } newTarget)
            EnsureComp<CETargetCompleteOwnedObjectiveMarkerComponent>(newTarget);
    }

    [SubscribeLocalEvent]
    private void OnValidateCandidate(Entity<CETargetCompleteOwnedObjectiveComponent> ent, ref CEValidateObjectiveTargetCandidateEvent args)
    {
        if (!_mind.TryGetMind(args.Candidate, out var mindId, out _))
            return;

        var objectiveCount = 0;
        foreach (var objective in _objectives.GetOwnedObjectives((mindId, null)))
        {
            if (_entityWhitelist.IsWhitelistPass(ent.Comp.ObjectiveBlacklist, objective))
                continue;

            ++objectiveCount;
        }

        if (objectiveCount <= 0)
            args.Invalidate();
    }

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<CETargetCompleteOwnedObjectiveComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        if (ent.Comp.TargetMind is not { } mind)
        {
            args.Progress = ent.Comp.DefaultProgress;
            return;
        }

        var incomplete = false;
        foreach (var objective in _objectives.GetOwnedObjectives((mind, null)))
        {
            if (_entityWhitelist.IsWhitelistPass(ent.Comp.ObjectiveBlacklist, objective))
                continue;

            incomplete |= !_objectives.IsCompleted(objective.AsNullable());
        }

        args.Progress = !incomplete ^ ent.Comp.Invert ? 1 : 0;
    }

    [SubscribeLocalEvent]
    private void OnObjectiveProgressChanged(ref CEObjectiveProgressChangedEvent ev)
    {
        if (HasComp<CETargetCompleteOwnedObjectiveComponent>(ev.Objective))
            return;

        if (_loop)
        {
            DebugTools.Assert($"Infinite loop detected in {nameof(CETargetCompleteObjectivesSystem)}!");
            return;
        }

        _loop = true;
        var query = EntityQueryEnumerator<CETargetCompleteOwnedObjectiveComponent>();
        while (query.MoveNext(out var uid, out _))
            _objectives.RefreshObjectiveProgress(uid);
        _loop = false;
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CETargetCompleteOwnedObjectiveMarkerComponent> ent, ref MapInitEvent args)
    {
        if (!_mind.TryGetMind(ent.Owner, out var mind, out _))
            return;

        foreach (var objective in _target.GetTargetingObjectives<CETargetCompleteOwnedObjectiveComponent>(ent.Owner))
            objective.Comp.TargetMind = mind;
    }

    [SubscribeLocalEvent]
    private void OnTargetMindGotAdded(Entity<CETargetCompleteOwnedObjectiveMarkerComponent> ent, ref MindAddedMessage args)
    {
        foreach (var objective in _target.GetTargetingObjectives<CETargetCompleteOwnedObjectiveComponent>(ent.Owner))
            objective.Comp.TargetMind = args.Mind;
    }

    [SubscribeLocalEvent]
    private void OnObjectivesChanged(Entity<CETargetCompleteOwnedObjectiveMarkerComponent> ent, ref CEObjectivesChangedEvent args)
    {
        foreach (var objective in _target.GetTargetingObjectives<CETargetCompleteOwnedObjectiveComponent>(ent.Owner))
            _objectives.RefreshObjectiveProgress(objective.Owner);
    }
}
