using Content.Shared._CE.Objectives.Target.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Shared._CE.Objectives.Target;

/// <summary>
/// Handles <see cref="CETargetPlayersObjectiveComponent"/>.
/// </summary>
public sealed partial class CETargetPlayersObjectiveSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    [SubscribeLocalEvent]
    private void OnGetObjectiveTargetCandidates(Entity<CETargetPlayersObjectiveComponent> ent, ref CEGetObjectiveTargetCandidatesEvent args)
    {
        // HumanoidProfileComponent is used to prevent mice, pAIs, etc from being chosen
        var query = EntityQueryEnumerator<HumanoidProfileComponent, MobStateComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out _, out var mobState, out var mindContainer))
        {
            // the player needs to have a mind and not be the holder itself, and be alive
            if (!_mind.TryGetMind(uid, out var mind, out _, mindContainer) ||
                mind == args.Holder.Owner ||
                !_mobState.IsAlive(uid, mobState))
                continue;

            args.Candidates.Add(uid);
        }
    }
}
