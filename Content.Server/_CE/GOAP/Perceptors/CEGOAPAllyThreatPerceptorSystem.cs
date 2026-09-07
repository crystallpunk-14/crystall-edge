using Content.Server._CE.GOAP.Actions;
using Content.Server._CE.GOAP.Classifiers;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.NPC.Systems;
using Robust.Shared.Player;

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

    public override void Initialize()
    {
        SubscribeLocalEvent<DamageableComponent, DamageDealtEvent>(OnAllyDamaged);
    }

    private void OnAllyDamaged(Entity<DamageableComponent> victim, ref DamageDealtEvent args)
    {
        if (args.Damage.GetTotal() <= 0 || args.Origin is not { } attacker || !Exists(attacker) || attacker == victim.Owner)
            return;

        var defendingAlly = !HasComp<ActorComponent>(attacker) &&
                            HasComp<CEGOAPThreatOnlyMeleeComponent>(attacker) &&
                            TryComp<CEGOAPKnowledgeCacheComponent>(attacker, out var attackerKnowledge) &&
                            attackerKnowledge.Enemies.Contains(victim.Owner);
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
