using Content.Shared._CE.Animation.Item.Components;
using Content.Shared._CE.MeleeWeapon;
using Content.Shared.Damage.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

public sealed partial class WeaponArcAttack : CEEntityEffectBase<WeaponArcAttack>
{
    [DataField]
    public float RangeMultiplier = 1f;

    [DataField]
    public float ArcWidth = 90f;

    [DataField]
    public Angle Angle = Angle.Zero;

    [DataField]
    public List<CEEntityEffect> Effects = new();

    /// <summary>
    /// When true, resolve the weapon from the off-hand instead of the active hand.
    /// Used in dual-wield animations to give each hand its own independent arc attack.
    /// </summary>
    [DataField]
    public bool UseOffHand = false;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("ce-entity-effect-guidebook-weapon-arc-attack", ("arcWidth", ArcWidth));
}

/// <summary>
/// Local event raised when an ArcAttack fires, used for debug visualization.
/// </summary>
public sealed partial class CEDebugArcAttackEvent(MapCoordinates position, Angle direction, float range, float arcWidth)
    : EntityEventArgs
{
    public MapCoordinates Position = position;
    public Angle Direction = direction;
    public float Range = range;
    public float ArcWidth = arcWidth;
}

public sealed partial class CEWeaponArcAttackEffectSystem : CEEntityEffectSystem<WeaponArcAttack>
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private CESharedWeaponSystem _melee = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    [Dependency] private EntityQuery<HandsComponent> _handsQuery = default!;

    /// <summary>
    /// Broad collision mask to hit mobs, items, machines, etc.
    /// Filtered by DamageableComponent afterwards.
    /// </summary>
    private const int ArcAttackMask = (int) (
        CollisionGroup.Opaque |
        CollisionGroup.Impassable |
        CollisionGroup.MidImpassable |
        CollisionGroup.HighImpassable |
        CollisionGroup.LowImpassable |
        CollisionGroup.BulletImpassable);

    /// <summary>
    /// Cast one ray every this many degrees across the arc.
    /// </summary>
    private const float DegreesPerRay = 5f;

    private const int MinRays = 3;

    protected override void Effect(ref CEEntityEffectEvent<WeaponArcAttack> args)
    {
        var used = args.Args.Used ?? args.Args.Source;

        if (args.Effect.UseOffHand && _handsQuery.TryComp(args.Args.Source, out var hands))
        {
            foreach (var handId in hands.SortedHands)
            {
                if (handId == hands.ActiveHandId)
                    continue;
                if (!_hands.TryGetHeldItem(args.Args.Source, handId, out var held))
                    continue;
                if (!HasComp<CEWeaponComponent>(held.Value))
                    continue;
                used = held.Value;
                break;
            }
        }

        TryComp<CEWeaponComponent>(used, out var weapon);

        // Scan Effects for a WeaponEffectSlot before raycasting.
        // If none is found (inline kick/ability), effects are applied directly.
        string? effectSlot = null;
        var effectPower = 1f;

        foreach (var effect in args.Effect.Effects)
        {
            if (effect is WeaponEffectSlot wes)
            {
                effectSlot = wes.Slot;
                effectPower = wes.Power;
                break;
            }
        }

        // If a slot name was found but the weapon doesn't define that slot, skip arc entirely.
        // This prevents magic staffs (OnSwing-only) from triggering melee hit effects.
        if (effectSlot != null && weapon != null && !weapon.EffectSlots.ContainsKey(effectSlot))
            return;

        var entityCoords = _transform.GetMapCoordinates(args.Args.Source);
        var direction = new Angle(args.Args.Angle.ToWorldVec()) + args.Effect.Angle;

        var range = (weapon?.Range ?? 1f) * args.Effect.RangeMultiplier;

        // Raise debug event for arc attack visualization
        var debugEvent = new CEDebugArcAttackEvent(entityCoords, direction, range, args.Effect.ArcWidth);
        EntityManager.EventBus.RaiseEvent(EventSource.Local, debugEvent);

        // Fan rays across the arc — same approach as vanilla MeleeWeaponSystem.ArcRayCast,
        // but with adaptive ray count: 1 ray per DegreesPerRay, minimum MinRays.
        // Note: range * 2 matches the original GetEntitiesInArc broadphase radius.
        var effectiveRange = range * 2;
        var arcWidthDeg = args.Effect.ArcWidth;
        var arcWidthRad = arcWidthDeg * Math.PI / 180.0;
        var rayCount = Math.Max(MinRays, (int) Math.Ceiling(arcWidthDeg / DegreesPerRay) + 1);
        var baseAngle = direction.Theta - arcWidthRad / 2;
        var increment = arcWidthRad / (rayCount - 1);

        var hitEntities = new HashSet<EntityUid>();

        for (var i = 0; i < rayCount; i++)
        {
            var castAngle = new Angle(baseAngle + increment * i);
            var ray = new CollisionRay(entityCoords.Position, castAngle.ToVec(), ArcAttackMask);

            foreach (var result in _physics.IntersectRay(
                         entityCoords.MapId,
                         ray,
                         effectiveRange,
                         args.Args.Source,
                         false))
            {
                hitEntities.Add(result.HitEntity);
            }
        }

        hitEntities.Remove(args.Args.Source);
        if (args.Args.Used is { } usedEntity)
            hitEntities.Remove(usedEntity);

        // Filter to only damageable entities.
        hitEntities.RemoveWhere(t => !HasComp<DamageableComponent>(t));

        // Filter out entities behind walls (line-of-sight check).
        // Use entity-based range check so fixture distance is respected for large colliders.
        var _args = args.Args;
        hitEntities.RemoveWhere(t =>
            !_interaction.InRangeUnobstructed(_args.Source, t, effectiveRange + 0.1f, overlapCheck: false));

        var targets = new List<EntityUid>(hitEntities);

        // Inline effects (kick, ability with no weapon slot): apply directly to each hit target.
        // Runs on both peers via the shared animation system.
        if (effectSlot == null && args.Effect.Effects.Count > 0)
        {
            foreach (var target in targets)
            {
                var inlineArgs = args.Args with { Target = target };
                foreach (var effect in args.Effect.Effects)
                {
                    effect.Effect(inlineArgs);
                }
            }
        }

        // Weapon-based attacks: route through HandleArcAttackHit for server validation.
        if (weapon != null)
            _melee.HandleArcAttackHit(args.Args.Source, (used, weapon), targets, effectSlot, effectPower);
    }
}
