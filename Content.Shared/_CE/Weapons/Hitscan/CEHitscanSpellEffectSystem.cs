
using Content.Shared._CE.Actions.Spells;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Whitelist;

namespace Content.Shared._CE.Weapons.Hitscan;

public sealed partial class CEHitscanSpellEffectSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEHitscanSpellEffectComponent, HitscanRaycastFiredEvent>(OnHitscanRaycastFired);
    }

    private void OnHitscanRaycastFired(Entity<CEHitscanSpellEffectComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        if (!_whitelist.CheckBoth(args.Data.HitEntity, ent.Comp.Whitelist, ent.Comp.Blacklist))
            return;

        foreach (var effect in ent.Comp.Effects)
        {
            effect.Effect(EntityManager, new CESpellEffectBaseArgs(args.Data.Shooter, args.Data.Gun, args.Data.HitEntity, null));
        }
    }
}
