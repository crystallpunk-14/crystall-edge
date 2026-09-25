using Content.Server._CE.GameTicking.Components;
using Content.Server._CE.Objectives.Components;
using Content.Shared._CE.DayCycle;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.Objectives.Components;

namespace Content.Server._CE.Objectives.Systems;

/// <summary>
/// Handles progress for <see cref="CEObjectiveMurkSphereCollapseConditionComponent"/> - the
/// inverse of <see cref="CEObjectiveLucsonSphereRestoreConditionSystem"/>. Progress tracks how
/// close the station is to the murk consuming the Lucson Sphere before the Light Monolith
/// restores it, based on <see cref="CEMurkConsumingRuleComponent"/>'s day countdown rather than
/// the monolith's charge - the monolith only matters for the terminal "already restored" case.
/// </summary>
public sealed partial class CEObjectiveMurkSphereCollapseConditionSystem : EntitySystem
{
    [Dependency] private CEObjectiveSystem _objectives = default!;

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
        // elapsed, independent of the monolith's charge (which only matters once it hits Fixed).
        var ruleQuery = EntityQueryEnumerator<CEMurkConsumingRuleComponent>();
        if (!ruleQuery.MoveNext(out _, out var rule) || rule.DaysToCollapse <= 0)
        {
            args.Progress = 1f;
            return;
        }

        args.Progress = (float) rule.DaysSinceCrack / rule.DaysToCollapse;
    }

    [SubscribeLocalEvent]
    private void OnSphereStateChanged(CEMurkSphereStateChangedEvent args)
    {
        RefreshAll();
    }

    [SubscribeLocalEvent]
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
