using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Physics;

namespace Content.Server._CE.RangedWeapon;

/// <summary>
/// Drops <see cref="TargetedProjectileComponent"/> once a projectile comes to rest: it only matters in flight,
/// and a lying arrow kept a stale Target that errored on every state send after the target was deleted.
/// </summary>
public sealed partial class CETargetedProjectileCleanupSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnSleep(Entity<TargetedProjectileComponent> ent, ref PhysicsSleepEvent args)
    {
        RemCompDeferred<TargetedProjectileComponent>(ent);
    }
}
