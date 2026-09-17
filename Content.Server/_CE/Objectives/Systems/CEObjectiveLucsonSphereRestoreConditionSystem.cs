using Content.Server._CE.Murk.SphereFixer;
using Content.Server._CE.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server._CE.Objectives.Systems;

/// <summary>
/// Handles progress for <see cref="CEObjectiveLucsonSphereRestoreConditionComponent"/> - taken
/// directly from the station's single Pillar of Light charge.
/// </summary>
public sealed partial class CEObjectiveLucsonSphereRestoreConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<CEObjectiveLucsonSphereRestoreConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var query = EntityQueryEnumerator<CEMurkSphereFixerComponent>();
        while (query.MoveNext(out _, out var fixer))
        {
            args.Progress = fixer.Charge;
            return;
        }

        args.Progress = 0f;
    }
}
