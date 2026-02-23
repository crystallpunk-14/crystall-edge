using Content.Shared._White.Weapons.Melee.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._White.Weapons.Melee.Systems;

public sealed class WhiteMeleeSelfDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhiteMeleeSelfDamageComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<WhiteMeleeSelfDamageComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        _damageable.TryChangeDamage(ent.Owner, ent.Comp.DamageToSelf);
    }
}
