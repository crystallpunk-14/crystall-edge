using Content.Shared.ActionBlocker;
using Content.Shared.Chasm;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Server._CE.ZLevels.EntitySystems;

public sealed partial class CEStationZLevelsSystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;

    private void InitChasm()
    {
        SubscribeLocalEvent<ChasmFallingComponent, CEChasmFallingEvent>(OnChasmFalling);
        SubscribeLocalEvent<ChasmFallingComponent, ComponentRemove>(OnCompRemoved);
    }

    private void OnCompRemoved(Entity<ChasmFallingComponent> ent, ref ComponentRemove args)
    {
        _blocker.UpdateCanMove(ent);
    }

    private void OnChasmFalling(Entity<ChasmFallingComponent> ent, ref CEChasmFallingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryMoveDown(ent))
            return;

        _stun.TryKnockdown(ent.Owner, TimeSpan.FromSeconds(8f));
        DamageSpecifier damage = new(_proto.Index<DamageTypePrototype>("Blunt"), 40f);
        _damageable.TryChangeDamage(ent.Owner, damage);

        RemCompDeferred<ChasmFallingComponent>(ent);
        args.Handled = true;
    }
}
