using Content.Shared._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Target.Components;
using Content.Shared.Whitelist;

namespace Content.Shared._CE.Objectives.Target;

/// <summary>
/// Handles <see cref="CEShareTargetObjectiveComponent"/>.
/// </summary>
public sealed partial class CEShareTargetObjectiveSystem : CEBaseObjectiveSystem<CEShareTargetObjectiveComponent>
{
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private CETargetObjectiveSystem _targetObjective = default!;

    protected override void InitializeObjective(Entity<CEShareTargetObjectiveComponent> ent, ref CEInitializeObjectiveEvent args)
    {
        TryResolveTarget(ent, args.Holder.AsNullable());
    }

    // Retries when the holder's own objective list changes (e.g. the source objective gets created
    // after us) and, globally, whenever any target-objective's target changes (e.g. the source
    // objective was created but its own target only resolved later via a retry elsewhere).
    [SubscribeLocalEvent]
    private void OnObjectivesChanged(Entity<CEObjectiveHolderComponent> ent, ref CEObjectivesChangedEvent args)
    {
        RetryHolder(ent.AsNullable());
    }

    [SubscribeLocalEvent]
    private void OnAnyTargetChanged(ref CEObjectiveTargetChangedEvent args)
    {
        var query = EntityQueryEnumerator<CEObjectiveHolderComponent>();
        while (query.MoveNext(out var holderUid, out var holderComp))
            RetryHolder((holderUid, holderComp));
    }

    private void RetryHolder(Entity<CEObjectiveHolderComponent?> holder)
    {
        foreach (var objective in ObjectivesSys.GetObjectives(holder))
        {
            if (!TryComp<CEShareTargetObjectiveComponent>(objective.Owner, out var share))
                continue;

            if (TryComp<CETargetObjectiveComponent>(objective.Owner, out var targetComp) && targetComp.Target != null)
                continue; // already resolved

            TryResolveTarget((objective.Owner, share), holder);
        }
    }

    private void TryResolveTarget(Entity<CEShareTargetObjectiveComponent> ent, Entity<CEObjectiveHolderComponent?> holder)
    {
        foreach (var objective in ObjectivesSys.GetObjectives(holder))
        {
            if (objective.Owner == ent.Owner)
                continue;

            if (!TryComp<CETargetObjectiveComponent>(objective.Owner, out var targetComp) || targetComp.Target == null)
                continue;

            if (_entityWhitelist.IsWhitelistFail(ent.Comp.ObjectiveWhitelist, objective.Owner))
                continue;

            _targetObjective.SetTarget(ent.Owner, targetComp.Target);
            break;
        }
    }
}
