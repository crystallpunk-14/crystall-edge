using System.Numerics;
using Content.Shared.Conveyor;
using Content.Shared.Examine;
using Content.Shared.Item;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._CE.Funnel;

public abstract partial class CESharedFunnelSystem : EntitySystem
{
    [Dependency] protected SharedMapSystem MapSystem = default!;
    [Dependency] protected SharedContainerSystem Container = default!;
    [Dependency] protected FixtureSystem Fixtures = default!;

    /// <summary>
    /// Picks the funnel mode from the conveyor anchored on its tile.
    /// </summary>
    public CEFunnelMode GetMode(Entity<CEFunnelComponent> ent, out Entity<ConveyorComponent>? conveyor)
    {
        conveyor = null;
        var xform = Transform(ent);

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return CEFunnelMode.Load;

        var tile = MapSystem.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var anchored = MapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (!TryComp<ConveyorComponent>(uid, out var conv))
                continue;

            conveyor = (uid.Value, conv);

            var convRot = Transform(uid.Value).LocalRotation + conv.Angle;
            if (conv.State == ConveyorState.Reverse)
                convRot += new Angle(MathF.PI);

            var front = xform.LocalRotation.RotateDir(Direction.North).ToVec();
            var dot = Vector2.Dot(convRot.ToWorldVec(), front);

            if (dot > 0.5f)
                return CEFunnelMode.Load;

            if (dot < -0.5f)
                return CEFunnelMode.Unload;

            return CEFunnelMode.Side;
        }

        return CEFunnelMode.Load;
    }

    /// <summary>
    /// True if the item matches any filter sample, or the funnel has no samples.
    /// </summary>
    public bool PassesFilter(Entity<CEFunnelComponent> ent, EntityUid item)
    {
        if (!Container.TryGetContainer(ent, ent.Comp.FilterContainerId, out var samples) ||
            samples.ContainedEntities.Count == 0)
            return true;

        foreach (var sample in samples.ContainedEntities)
        {
            if (IsSameKind(sample, item))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Stackable items match by stack type, everything else by prototype id.
    /// </summary>
    private bool IsSameKind(EntityUid sample, EntityUid item)
    {
        var sampleStack = CompOrNull<StackComponent>(sample);
        var itemStack = CompOrNull<StackComponent>(item);

        if (sampleStack is not null || itemStack is not null)
            return sampleStack?.StackTypeId == itemStack?.StackTypeId;

        var sampleProto = MetaData(sample).EntityPrototype?.ID;
        return sampleProto is not null && sampleProto == MetaData(item).EntityPrototype?.ID;
    }

    [SubscribeLocalEvent]
    private void OnPreventCollide(Entity<CEFunnelComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (Fixtures.GetFixtureOrNull(ent, ent.Comp.FixtureId) != args.OurFixture)
            return;

        // The front edge only stops items, and only while loading
        if (!HasComp<ItemComponent>(args.OtherEntity) || GetMode(ent, out _) != CEFunnelMode.Load)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<CEFunnelComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(CEFunnelComponent)))
        {
            var mode = GetMode(ent, out _);
            args.PushMarkup(Loc.GetString("ce-funnel-examine-mode", ("mode", mode.ToString())));

            if (!Container.TryGetContainer(ent, ent.Comp.FilterContainerId, out var samples) ||
                samples.ContainedEntities.Count == 0)
            {
                args.PushMarkup(Loc.GetString("ce-funnel-examine-no-filter"));
                return;
            }

            var names = new List<string>();
            foreach (var sample in samples.ContainedEntities)
            {
                names.Add(Name(sample));
            }

            args.PushMarkup(Loc.GetString("ce-funnel-examine-filter", ("items", string.Join(", ", names))));
        }
    }
}
