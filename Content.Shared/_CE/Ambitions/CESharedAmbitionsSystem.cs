using Content.Shared._CE.Ambitions.Components;
using Robust.Shared.Timing;

namespace Content.Shared._CE.Ambitions;

public abstract class CESharedAmbitionsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEAmbitionsSetupComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<CEAmbitionsSetupComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.EndTime = _timing.CurTime + ent.Comp.AvailableTime;
        DirtyField(ent,  ent.Comp, nameof(CEAmbitionsSetupComponent.EndTime));
    }
}
