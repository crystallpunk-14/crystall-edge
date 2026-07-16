using Content.Shared._CE.ZLevels.Core;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;

namespace Content.Shared._CE.EntityEffect.Effects;

/// <summary>
/// Deals vanilla typed damage to the resolved target entity via <see cref="DamageableSystem"/>.
/// </summary>
public sealed partial class Damage : CEEntityEffectBase<Damage>
{
    [DataField("damage", required: true)]
    public DamageSpecifier DamageSpec = new();

    [DataField]
    public bool IgnoreResistances;

    [DataField]
    public bool InterruptDoAfters = true;

    /// <summary>
    /// Whether to flash the target's sprite red when this effect deals damage.
    /// </summary>
    [DataField]
    public bool ColorFlash = true;
}

public sealed partial class CEDamageEffectSystem : CEEntityEffectSystem<Damage>
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedColorFlashEffectSystem _colorFlash = default!;

    protected override void Effect(ref CEEntityEffectEvent<Damage> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        var damage = args.Effect.DamageSpec * args.Args.Power;

        var dealt = _damageable.TryChangeDamage(
            entity,
            damage,
            args.Effect.IgnoreResistances,
            args.Effect.InterruptDoAfters,
            origin: args.Args.Source);

        if (dealt && args.Effect.ColorFlash)
        {
            var filter = CEFilter.ZPvsExcept(args.Args.Source, EntityManager);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { entity }, filter);
        }
    }
}
