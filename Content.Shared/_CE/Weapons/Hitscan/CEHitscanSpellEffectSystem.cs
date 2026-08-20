using Content.Shared._CE.EntityEffect;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Whitelist;

namespace Content.Shared._CE.Weapons.Hitscan;

public sealed partial class CEHitscanSpellEffectSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEHitscanEntityEffectComponent, HitscanRaycastFiredEvent>(OnHitscanRaycastFired);
    }

    private void OnHitscanRaycastFired(Entity<CEHitscanEntityEffectComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

        if (!_whitelist.CheckBoth(args.Data.HitEntity, ent.Comp.Whitelist, ent.Comp.Blacklist))
            return;

        var effectArgs = new CEEntityEffectArgs(
            EntityManager,
            args.Data.Shooter ?? ent.Owner,
            args.Data.Gun,
            Angle.Zero,
            1f,
            args.Data.HitEntity,
            Transform(ent).Coordinates);

        foreach (var effect in ent.Comp.Effects)
        {
            effect.Effect(effectArgs);
        }
    }
}
