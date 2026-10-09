using Content.Server._CE.GameTicking;
using Content.Server._CE.GameTicking.Components;
using Content.Server._CE.Objectives.Components;
using Content.Shared._CE.DayCycle;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.Objectives.Components;

namespace Content.Server._CE.Objectives.Systems;

/// <summary>
/// Handles progress for <see cref="CEObjectiveMurkSphereCollapseConditionComponent"/> - the
/// inverse of <see cref="CEObjectiveLucsonSphereRestoreConditionSystem"/>. Progress tracks how
/// close the station is to the murk consuming the Lucson Sphere before the city restores it,
/// based on <see cref="CEMurkConsumingRuleComponent"/>'s collapse countdown - restoration only
/// matters for the terminal "already restored" case.
/// </summary>
public sealed partial class CEObjectiveMurkSphereCollapseConditionSystem : EntitySystem
{
    [Dependency] private CEObjectiveSystem _objectives = default!;
    [Dependency] private CEMurkConsumingRuleSystem _murkConsuming = default!;

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<CEObjectiveMurkSphereCollapseConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        var sphereQuery = EntityQueryEnumerator<CEMurkLusconSphereComponent>();
        if (!sphereQuery.MoveNext(out _, out var sphere))
        {
            args.Progress = 0f;
            return;
        }

        switch (sphere.State)
        {
            case CEMurkSphereState.Collapsing:
                args.Progress = 1f;
                return;
            case CEMurkSphereState.Stable:
            case CEMurkSphereState.Fixed:
                args.Progress = 0f;
                return;
        }

        // Cracked: the race is on - progress follows how much of the collapse countdown has
        // elapsed (restoration only matters once the sphere hits Fixed).
        var ruleQuery = EntityQueryEnumerator<CEMurkConsumingRuleComponent>();
        if (!ruleQuery.MoveNext(out _, out var rule))
        {
            args.Progress = 1f;
            return;
        }

        args.Progress = _murkConsuming.GetCollapseProgress(rule);
    }

    [SubscribeLocalEvent]
    private void OnSphereStateChanged(CEMurkSphereStateChangedEvent args)
    {
        RefreshAll();
    }

    [SubscribeLocalEvent(after: [typeof(CEMurkConsumingRuleSystem)])]
    private void OnStartDay(CEStartDayEvent args)
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        var query = EntityQueryEnumerator<CEObjectiveMurkSphereCollapseConditionComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _objectives.RefreshObjectiveProgress(uid);
        }
    }
}
