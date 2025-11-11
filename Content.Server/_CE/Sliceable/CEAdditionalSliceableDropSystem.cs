using Content.Server.Stack;
using Content.Shared.Nutrition;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server._CE.Sliceable;

public sealed partial class CEAdditionalSliceableDropSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StackSystem _stack = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEAdditionalSliceableDropComponent, SliceFoodDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<CEAdditionalSliceableDropComponent> ent, ref SliceFoodDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var xform = Transform(ent);
        var pos = xform.Coordinates;
        foreach (var (proto, count) in ent.Comp.Loot)
        {
            for (var i = 0; i < count; i++)
            {
                var spawned = SpawnAtPosition(proto, pos);
                _transform.SetLocalRotation(spawned, _random.NextAngle());
                _stack.TryMergeToContacts(spawned, xform: xform);
            }
        }
    }
}
