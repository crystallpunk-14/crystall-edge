/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.ZLevels.Core;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Robust.Shared.Analyzers;

namespace Content.Shared._CE.ZLevels.Damage.FallingDamage;

public sealed partial class CEFallingDamageSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedColorFlashEffectSystem _colorFlash = default!;

    [SubscribeLocalEvent]
    private void OnFallOnMe(Entity<CEFallingDamageComponent> ent, ref CEZFellOnMeEvent args)
    {
        if (_damageable.TryChangeDamage(args.Fallen, ent.Comp.Damage * args.Speed))
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { args.Fallen }, CEFilter.ZPvsExcept(args.Fallen, EntityManager));
    }
}
