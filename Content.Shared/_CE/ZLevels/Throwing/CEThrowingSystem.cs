using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using static Content.Shared._CE.ZLevels.Core.EntitySystems.CESharedZLevelsSystem;
using System.Numerics;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._CE.ZLevels.Throwing;

public sealed class CEThrowingSystem : EntitySystem
{
    [Dependency] private readonly CESharedZLevelsSystem _zLevel = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    public const float ZlayerHeightUnit = 1f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEZLevelViewerComponent, ThrowEvent>(OnThrow);
    }

    public double NeededUpForce(float currentHeight, float targetHeight)
    {
        double result = 0;

        var displacement = (targetHeight - currentHeight) * ZlayerHeightUnit;
        var gravity = CESharedZLevelsSystem.ZGravityForce;

        if (displacement <= 0) return result;

        //H = v0^2/g - v0^2/g2
        //H/v0^2 = 1/g - 1/g2
        //H/v0^2 = 1/g2
        //v0^2/H = g2
        //v0^2= Hg2
        //v0= root(Hg2)

        result = Math.Sqrt(displacement * gravity * 2);

        return result;
    }
    /// <summary>
    /// If you look up and throw something, you will throw it up by the selected z-level.
    /// </summary>
    private void OnThrow(Entity<CEZLevelViewerComponent> ent, ref ThrowEvent args)
    {
        if (!ent.Comp.LookUp)
            return;

        if (!TryComp<CEZPhysicsComponent>(args.Thrown, out var thrownZPhys))
            return;

        if (!TryComp<PhysicsComponent>(args.Thrown, out var thrownPhys))
            return;

        var oldVelocity = thrownPhys.LinearVelocity;
        var newVelocity = new Vector2(oldVelocity.X, oldVelocity.Y - ZLevelOffset * ent.Comp.ViewedZLevel);   //no idea if this is correct the offset should be substracted from the y of the target point of the throw but we only get the finished velocity

        var zVelocity = NeededUpForce(0, ent.Comp.ViewedZLevel);

        _physics.SetLinearVelocity(args.Thrown, newVelocity);

        _zLevel.AddZVelocity((args.Thrown, thrownZPhys), (float)zVelocity);

    }
}
