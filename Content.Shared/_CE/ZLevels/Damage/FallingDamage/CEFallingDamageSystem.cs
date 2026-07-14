/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.Health;
using Content.Shared._CE.TileEffects.Core;

namespace Content.Shared._CE.ZLevels.Damage.FallingDamage;

public sealed partial class CEFallingDamageSystem : EntitySystem
{
    [Dependency] private CESharedDamageableSystem _damageable = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEFallingDamageComponent, CEZFellOnMeEvent>(OnFallOnMe);
    }

    private void OnFallOnMe(Entity<CEFallingDamageComponent> ent, ref CEZFellOnMeEvent args)
    {
        EntityUid? source = null;
        if (TryComp<CETileEffectComponent>(ent, out var tileEffect))
            source = tileEffect.Source;

        _damageable.TakeDamage(args.Fallen, ent.Comp.Damage * args.Speed, source: source);
    }
}
