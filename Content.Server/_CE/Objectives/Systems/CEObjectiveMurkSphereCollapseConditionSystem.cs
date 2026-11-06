using Content.Server._CE.GameTicking;
using Content.Server._CE.GameTicking.Components;
using Content.Server._CE.Objectives.Components;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.Objectives.Components;

namespace Content.Server._CE.Objectives.Systems;

/// <summary>
/// Handles progress for <see cref="CEObjectiveMurkSphereCollapseConditionComponent"/> - the
/// inverse of <see cref="CEObjectiveLucsonSphereRestoreConditionSystem"/>. Progress tracks how
/// close the station is to the murk consuming the Lucson Sphere before the city restores it,
/// based on <see cref="CEMurkConsumingRuleComponent.MurkLevel"/> - restoration only
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
            case CEMurkSphereState.Failure:
                args.Progress = 1f;
                return;
            case CEMurkSphereState.PreRound:
            case CEMurkSphereState.Success:
                args.Progress = 0f;
                return;
        }

        // InGame or Ritual: progress follows the murk level (frozen during the ritual;
        // restoration only matters once the sphere hits Success).
        args.Progress = _murkConsuming.TryGetMurkLevel(out var level) ? level : 1f;
    }

    [SubscribeLocalEvent]
    private void OnSphereStateChanged(CEMurkSphereStateChangedEvent args)
    {
        RefreshAll();
    }

    [SubscribeLocalEvent]
    private void OnMurkLevelChanged(CEMurkLevelChangedEvent args)
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
