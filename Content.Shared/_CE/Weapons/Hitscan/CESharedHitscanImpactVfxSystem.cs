using Content.Shared._CE.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Analyzers;

namespace Content.Shared._CE.Weapons.Hitscan;

public sealed partial class CESharedHitscanImpactVfxSystem : EntitySystem
{

    [SubscribeLocalEvent]
    private void OnHitscanFired(Entity<CEHitscanImpactVfxComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        SpawnAtPosition(ent.Comp.Vfx, args.Data.HitCoordinates);
    }
}
