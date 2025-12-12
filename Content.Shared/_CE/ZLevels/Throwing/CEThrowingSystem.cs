using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Throwing;

namespace Content.Shared._CE.ZLevels.Throwing;

public sealed class CEThrowingSystem : EntitySystem
{
    [Dependency] private readonly CESharedZLevelsSystem _zLevel = default!;

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<CEZLevelViewerComponent, ThrowEvent>(OnThrow); //Temporaly disabled because bugs
    }

    /// <summary>
    /// If you look up and throw something, you will throw it up by 1 z-level.
    /// </summary>
    private void OnThrow(Entity<CEZLevelViewerComponent> ent, ref ThrowEvent args)
    {
        if (!ent.Comp.LookUp)
            return;

        if (!TryComp<CEZPhysicsComponent>(args.Thrown, out var thrownZPhys))
            return;

        _zLevel.AddZVelocity((args.Thrown, thrownZPhys), ent.Comp.ThrowUpForce);
    }
}
