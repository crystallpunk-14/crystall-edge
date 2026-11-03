using Content.Shared._CE.Workbench.Press;
using Robust.Shared.Timing;

namespace Content.Server._CE.Workbench.Press;

/// <summary>
/// Mirrors autocrafter progress into <see cref="CEPressVisualsComponent"/> so clients can animate the press block.
/// </summary>
public sealed partial class CEPressVisualsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnCraftStarted(Entity<CEPressVisualsComponent> ent, ref CEAutoCraftStartedEvent args)
    {
        ent.Comp.CraftStartTime = _timing.CurTime;
        ent.Comp.CraftEndTime = _timing.CurTime + args.Duration;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnCraftFinished(Entity<CEPressVisualsComponent> ent, ref CEAutoCraftFinishedEvent args)
    {
        ent.Comp.CraftStartTime = TimeSpan.Zero;
        ent.Comp.CraftEndTime = TimeSpan.Zero;
        ent.Comp.LastCrushTime = _timing.CurTime;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnCraftCancelled(Entity<CEPressVisualsComponent> ent, ref CEAutoCraftCancelledEvent args)
    {
        if (ent.Comp.CraftEndTime == TimeSpan.Zero)
            return;

        ent.Comp.CraftStartTime = TimeSpan.Zero;
        ent.Comp.CraftEndTime = TimeSpan.Zero;
        Dirty(ent);
    }
}
