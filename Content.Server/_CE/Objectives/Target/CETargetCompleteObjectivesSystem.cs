using Content.Server._CE.Objectives.Target.Components;
using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Target;
using Content.Shared._CE.Objectives.Target.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Utility;

namespace Content.Server._CE.Objectives.Target;

/// <summary>
/// Handles <see cref="CETargetCompleteOwnedObjectiveComponent"/>.
/// </summary>
public sealed partial class CETargetCompleteObjectivesSystem : CEBaseTargetObjectiveSystem<CETargetCompleteOwnedObjectiveComponent>
{
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;

    public override Type[] TargetRelayComponents { get; } = [typeof(CETargetCompleteOwnedObjectiveMarkerComponent)];

    // Refreshing every CETargetCompleteOwnedObjectiveComponent whenever any objective's progress
    // changes could re-enter this same condition's own progress change - guard against that.
    private bool _loop;

    [SubscribeLocalEvent]
    private void OnValidateCandidate(Entity<CETargetCompleteOwnedObjectiveComponent> ent, ref CEValidateObjectiveTargetCandidateEvent args)
    {
        if (!MindSys.TryGetMind(args.Candidate, out var mindId, out _))
            return;

        var objectiveCount = 0;
        foreach (var objective in ObjectivesSys.GetOwnedObjectives((mindId, null)))
        {
            if (_entityWhitelist.IsWhitelistPass(ent.Comp.ObjectiveBlacklist, objective))
                continue;

            ++objectiveCount;
        }

        if (objectiveCount <= 0)
            args.Invalidate();
    }

    protected override void GetObjectiveProgress(Entity<CETargetCompleteOwnedObjectiveComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        if (ent.Comp.TargetMind is not { } mind)
        {
            args.Progress = ent.Comp.DefaultProgress;
            return;
        }

        var incomplete = false;
        foreach (var objective in ObjectivesSys.GetOwnedObjectives((mind, null)))
        {
            if (_entityWhitelist.IsWhitelistPass(ent.Comp.ObjectiveBlacklist, objective))
                continue;

            incomplete |= !ObjectivesSys.IsCompleted(objective.AsNullable());
        }

        args.Progress = !incomplete ^ ent.Comp.Invert ? 1 : 0;
    }

    [SubscribeLocalEvent]
    private void OnObjectiveProgressChanged(ref CEObjectiveProgressChangedEvent ev)
    {
        if (HasComp<CETargetCompleteOwnedObjectiveComponent>(ev.Objective))
            return;

        // This really shouldn't be necessary but I don't want to accidentally create an infinite
        // loop here if something's messed up.
        if (_loop)
        {
            DebugTools.Assert($"Infinite loop detected in {nameof(CETargetCompleteObjectivesSystem)}!");
            return;
        }

        _loop = true;
        var query = EntityQueryEnumerator<CETargetCompleteOwnedObjectiveComponent>();
        while (query.MoveNext(out var uid, out _))
            ObjectivesSys.RefreshObjectiveProgress(uid);
        _loop = false;
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CETargetCompleteOwnedObjectiveMarkerComponent> ent, ref MapInitEvent args)
    {
        if (!MindSys.TryGetMind(ent.Owner, out var mind, out _))
            return;

        foreach (var objective in GetTargetingObjectives(ent))
            objective.Comp.TargetMind = mind;
    }

    [SubscribeLocalEvent]
    private void OnTargetMindGotAdded(Entity<CETargetCompleteOwnedObjectiveMarkerComponent> ent, ref MindAddedMessage args)
    {
        foreach (var objective in GetTargetingObjectives(ent))
            objective.Comp.TargetMind = args.Mind;
    }

    [SubscribeLocalEvent]
    private void OnObjectivesChanged(Entity<CETargetCompleteOwnedObjectiveMarkerComponent> ent, ref CEObjectivesChangedEvent args)
    {
        RefreshTargetingObjectives(ent);
    }
}
