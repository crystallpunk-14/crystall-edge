using Content.Server._CE.Murk.SphereFixer;
using Content.Server._CE.Objectives.Components;
using Content.Shared._CE.Objectives.Components;

namespace Content.Server._CE.Objectives.Systems;

/// <summary>
/// Handles progress for <see cref="CEObjectiveLucsonSphereRestoreConditionComponent"/> - taken
/// directly from the station's single Light Monolith charge.
/// </summary>
public sealed partial class CEObjectiveLucsonSphereRestoreConditionSystem : EntitySystem
{
    [Dependency] private CEObjectiveSystem _objectives = default!;

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<CEObjectiveLucsonSphereRestoreConditionComponent> ent, ref CEGetObjectiveProgressEvent args)
    {
        var query = EntityQueryEnumerator<CEMurkSphereFixerComponent>();
        while (query.MoveNext(out _, out var fixer))
        {
            args.Progress = fixer.Charge;
            return;
        }

        args.Progress = 0f;
    }

    // Charge changes every tick, but the objective's networked progress only needs to catch up
    // to it every RefreshInterval - the character menu doesn't need per-tick precision.
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEObjectiveLucsonSphereRestoreConditionComponent>();
        while (query.MoveNext(out var uid, out var condition))
        {
            condition.NextRefresh -= TimeSpan.FromSeconds(frameTime);
            if (condition.NextRefresh > TimeSpan.Zero)
                continue;

            condition.NextRefresh = condition.RefreshInterval;
            _objectives.RefreshObjectiveProgress(uid);
        }
    }
}
