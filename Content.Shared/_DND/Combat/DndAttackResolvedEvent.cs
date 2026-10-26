using Content.Shared.Damage;

namespace Content.Shared._DND.Combat;

/// <summary>
/// Raised on the attacker after an attack roll is resolved, including a miss.
/// </summary>
[ByRefEvent]
public struct DndAttackResolvedEvent
{
    public EntityUid Attacker;
    public EntityUid Target;
    public EntityUid Weapon;
    public DndWeaponProfile Profile;
    public int AbilityScore;
    public DndAttackOutcome Outcome;
    public bool AllowCleave;
    public bool AllowNick;
    public int Depth;
    public DamageSpecifier Damage;
}
