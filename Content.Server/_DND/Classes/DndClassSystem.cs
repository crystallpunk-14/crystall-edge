using Content.Shared._DND.Classes;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Server._DND.Classes;

/// <summary>
/// Records the chosen class on the spawned body. NPC never reaches this event.
/// </summary>
public sealed partial class DndClassSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    [SubscribeLocalEvent]
    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!_prototypes.TryIndex(ev.Profile.DndClass, out DndClassPrototype? dndClass) || dndClass.SpawnAsGhost)
            return;

        var comp = EnsureComp<DndClassComponent>(ev.Mob);
        comp.Class = dndClass.ID;
        Dirty(ev.Mob, comp);
    }
}
