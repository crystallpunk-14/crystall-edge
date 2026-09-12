using System.Numerics;
using Content.Server._CE.MeleeWeapon;
using Content.Shared._CE.Animation.Item.Components;
using Content.Shared._CE.GOAP;
using Content.Shared._CE.GOAP.Components;
using Content.Shared.CombatMode;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;

namespace Content.Server._CE.GOAP.Actions;

/// <summary>
/// Performs a melee attack on the current target.
/// </summary>
public sealed partial class CEGOAPMeleeAttackAction : CEGOAPActionBase<CEGOAPMeleeAttackAction>
{
    [DataField]
    public CEUseType UseType = CEUseType.Primary;

    /// <summary>
    /// Random angle spread for attacks in degrees.
    /// </summary>
    [DataField]
    public float AngleVariation = 15f;
}

public sealed partial class CEGOAPMeleeAttackActionSystem : CEGOAPActionSystem<CEGOAPMeleeAttackAction>
{
    [Dependency] private CEWeaponSystem _weapon = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

    protected override void OnActionStartup(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionStartupEvent<CEGOAPMeleeAttackAction> args)
    {
        _combatMode.SetInCombatMode(ent, true);
    }

    protected override void OnActionUpdate(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionUpdateEvent<CEGOAPMeleeAttackAction> args)
    {
        if (args.Action.Selector == null)
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        var result = args.Action.Selector.Resolve(ent, EntityManager);
        if (result.Entity is not { } target)
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        // Check if target is neutralized
        if (TryComp<MobStateComponent>(target, out var targetMobState) && _mobState.IsIncapacitated(target, targetMobState))
        {
            args.Status = CEGOAPActionStatus.Finished;
            return;
        }

        if (!_weapon.TryGetWeapon(ent, out var weapon))
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        if (!_xformQuery.TryGetComponent(ent, out var xform) ||
            !_xformQuery.TryGetComponent(target, out var targetXform))
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        if (!xform.Coordinates.TryDistance(EntityManager, targetXform.Coordinates, out var distance))
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        // In range: attack
        var ownerPos = _transform.GetWorldPosition(xform);
        var targetPos = _transform.GetWorldPosition(targetXform);
        var direction = targetPos - ownerPos;
        var angle = direction == Vector2.Zero
            ? Angle.Zero
            : Angle.FromWorldVec(direction);
        angle += Angle.FromDegrees(
            _random.NextFloat(-args.Action.AngleVariation, args.Action.AngleVariation));

        if (!_weapon.TryUse(ent, weapon.Value, args.Action.UseType, angle))
        {
            args.Status = CEGOAPActionStatus.Failed;
            return;
        }

        args.Status = CEGOAPActionStatus.Running;
    }

    protected override void OnActionShutdown(
        Entity<CEGOAPComponent> ent,
        ref CEGOAPActionShutdownEvent<CEGOAPMeleeAttackAction> args)
    {
        _combatMode.SetInCombatMode(ent, false);
    }
}