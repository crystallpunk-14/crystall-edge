using System.Linq;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Drill;

public abstract class CESharedDrillSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;

    List<EntityUid> _temp = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEDrillComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var drill, out var xform))
        {
            if (!drill.Enabled)
                continue;
            if (_timing.CurTime < drill.NextDamageTime)
                continue;

            drill.NextDamageTime = _timing.CurTime + drill.DamageFrequency;

            var pos = _transform.GetWorldPosition(uid);
            var direction = _transform.GetWorldRotation(uid);
            var distance = drill.Distance;

            var ray = new CollisionRay(pos, direction.ToWorldVec(), drill.CollisionMask);
            var rayCastResults = _physics.IntersectRay(xform.MapID, ray, distance, uid, returnOnFirstHit: false).ToList();

            if (!rayCastResults.Any())
                continue;

            _temp.Clear();
            foreach (var hit in rayCastResults)
            {
                _damageable.TryChangeDamage(hit.HitEntity, drill.Damage, false, true, uid);
                _temp.Add(hit.HitEntity);
            }

            _color.RaiseEffect(Color.Red, _temp, Filter.Pvs(uid, entityManager: EntityManager));
        }
    }
}
