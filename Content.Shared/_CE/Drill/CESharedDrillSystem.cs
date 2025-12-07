using System.Linq;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.Jittering;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Drill;

/// <summary>
/// Handles the automatic drilling behavior for stationary drills, including damage application and effects.
/// </summary>
public abstract class CESharedDrillSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly MeleeSoundSystem _meleeSound = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly List<EntityUid> _cachedEntityList = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEDrillComponent, MeleeWeaponComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var drill, out var melee, out var xform))
        {
            if (!drill.Enabled)
                continue;
            if (!_timing.IsFirstTimePredicted)
                continue;
            if (_timing.CurTime < drill.NextDamageTime)
                continue;

            var freq = TimeSpan.FromSeconds(1 / melee.AttackRate);
            drill.NextDamageTime = _timing.CurTime + freq;

            var pos = _transform.GetWorldPosition(uid);
            var direction = _transform.GetWorldRotation(uid);
            var distance = melee.Range;

            var ray = new CollisionRay(pos, direction.ToWorldVec(), drill.CollisionMask);
            var rayCastResults = _physics.IntersectRay(xform.MapID, ray, distance, uid, returnOnFirstHit: false).ToList();

            if (!rayCastResults.Any())
                continue;

            _cachedEntityList.Clear();
            foreach (var hit in rayCastResults)
            {
                _damageable.TryChangeDamage(hit.HitEntity, melee.Damage, false, true, uid);

                _meleeSound.PlayHitSound(hit.HitEntity, uid, SharedMeleeWeaponSystem.GetHighestDamageSound(melee.Damage, _proto), null, melee);
                _cachedEntityList.Add(hit.HitEntity);
            }

            _color.RaiseEffect(Color.Red, _cachedEntityList, Filter.Pvs(uid, entityManager: EntityManager));
            _jitter.DoJitter(uid, freq, true, drill.JitterAmplitude, drill.JitterFreq);
        }
    }
}
