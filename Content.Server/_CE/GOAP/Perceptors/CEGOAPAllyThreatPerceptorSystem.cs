using Content.Server._CE.GOAP.Combat;
using Content.Server._CE.GOAP.Classifiers;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.NPC.Systems;

namespace Content.Server._CE.GOAP.Perceptors;

/// <summary>Witnesses attacks against nearby allies and remembers the attacker.</summary>
[RegisterComponent]
public sealed partial class CEGOAPAllyThreatPerceptorComponent : Component
{
    [DataField] public float Range = 6f;
}

public sealed partial class CEGOAPAllyThreatPerceptorSystem : EntitySystem
{
    [Dependency] private CEGOAPPainPerceptorSystem _pain = default!;
    [Dependency] private NpcFactionSystem _faction = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private CEGOAPDefensiveCombatSystem _defense = default!;

    [SubscribeLocalEvent]
    private void OnAllyDamaged(Entity<DamageableComponent> victim, ref DamageDealtEvent args)
    {
        if (args.Damage.GetTotal() <= 0 || args.Origin is not { } attacker || !Exists(attacker) || attacker == victim.Owner)
            return;

        var defendingAlly = _defense.IsDefendingAgainst(attacker, victim.Owner);
        var query = EntityQueryEnumerator<CEGOAPAllyThreatPerceptorComponent, CEGOAPPainPerceptorComponent, CEGOAPKnowledgeCacheComponent>();
        while (query.MoveNext(out var uid, out var witness, out var pain, out var knowledge))
        {
            if (uid == victim.Owner || uid == attacker ||
                knowledge.Enemies.Contains(victim.Owner) ||
                !_faction.IsEntityFriendly(uid, victim.Owner) ||
                !_examine.InRangeUnOccluded(uid, victim.Owner, witness.Range))
                continue;

            // A witness may arrive after the original provocation. Share a trusted NPC defender's
            // threat instead of mistaking its restricted melee hit for aggression against an ally.
            if (defendingAlly && _faction.IsEntityFriendly(uid, attacker))
            {
                _pain.ReportThreat((uid, pain), victim.Owner);
                continue;
            }

            _pain.ReportThreat((uid, pain), attacker);
        }
    }
}
