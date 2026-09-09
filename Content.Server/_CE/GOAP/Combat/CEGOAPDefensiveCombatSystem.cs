using Content.Server._CE.GOAP.Classifiers;
using Content.Shared._CE.MeleeWeapon;
using Robust.Shared.Player;

namespace Content.Server._CE.GOAP.Combat;

public sealed partial class CEGOAPDefensiveCombatSystem : EntitySystem
{
    /// <summary>Lets witnesses recognize a defender even after the original provocation is no longer visible.</summary>
    public bool IsDefendingAgainst(EntityUid attacker, EntityUid target)
    {
        return !HasComp<ActorComponent>(attacker) &&
               HasComp<CEGOAPDefensiveCombatComponent>(attacker) &&
               TryComp<CEGOAPKnowledgeCacheComponent>(attacker, out var knowledge) &&
               knowledge.Enemies.Contains(target);
    }

    [SubscribeLocalEvent]
    private void OnArcTargets(Entity<CEGOAPDefensiveCombatComponent> ent, ref CEWeaponArcTargetsEvent args)
    {
        if (HasComp<ActorComponent>(ent))
            return;

        // The animation may finish after its GOAP action. Keep the hit policy on the actor,
        // otherwise a finishing defensive swing can provoke nearby allies into fighting.
        if (!TryComp<CEGOAPKnowledgeCacheComponent>(ent, out var knowledge))
        {
            args.Targets.Clear();
            return;
        }

        args.Targets.RemoveAll(target => !knowledge.Enemies.Contains(target));
    }
}
