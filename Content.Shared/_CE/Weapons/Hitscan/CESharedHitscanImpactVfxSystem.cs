using Content.Shared._CE.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;

namespace Content.Shared._CE.Weapons.Hitscan;

public sealed partial class CESharedHitscanImpactVfxSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEHitscanImpactVfxComponent, HitscanRaycastFiredEvent>(OnHitscanFired);
    }

    private void OnHitscanFired(Entity<CEHitscanImpactVfxComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        SpawnAtPosition(ent.Comp.Vfx, args.Data.HitCoordinates);
    }
}
