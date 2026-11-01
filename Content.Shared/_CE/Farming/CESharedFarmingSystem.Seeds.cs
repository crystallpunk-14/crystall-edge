using Content.Shared._CE.Farming.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Stacks;
using Robust.Shared.Analyzers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Farming;

public abstract partial class CESharedFarmingSystem
{

    [SubscribeLocalEvent]
    private void OnSeedExamine(Entity<CESeedComponent> ent, ref ExaminedEvent args)
    {
        if (!_proto.Resolve(ent.Comp.PlantProto, out var plantProto))
            return;

        args.PushMarkup(Loc.GetString("ce-farming-seed-examine", ("name", plantProto.Name)));
    }

    [SubscribeLocalEvent]
    private void OnSeedInteract(Entity<CESeedComponent> seed, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target is not null)
            return;

        if (!CanPlant(seed.Comp.PlantProto, args.ClickLocation, args.User))
            return;

        var doAfterArgs =
            new DoAfterArgs(EntityManager,
                args.User,
                seed.Comp.PlantingTime,
                new CEPlantSeedDoAfterEvent(GetNetCoordinates(args.ClickLocation)),
                seed)
            {
                BreakOnDamage = true,
                BlockDuplicate = false,
                CancelDuplicate = false,
                BreakOnMove = true,
                BreakOnHandChange = true,
            };
        _doAfter.TryStartDoAfter(doAfterArgs);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnSeedPlantedDoAfter(Entity<CESeedComponent> ent, ref CEPlantSeedDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var position = GetCoordinates(args.Coordinates);
        if (!CanPlant(ent.Comp.PlantProto, position, args.User))
            return;

        args.Handled = true;

        PredictedSpawnAtPosition(ent.Comp.PlantProto, position);

        if (TryComp<StackComponent>(ent, out var stack) && stack.Count > 1)
            _stack.SetCount((ent.Owner, stack), stack.Count - 1);
        else
            PredictedQueueDel(ent);
    }

    protected bool CanPlant(EntityPrototype plant, EntityCoordinates position, EntityUid? user, EntityUid? exclude = null)
    {
        var map = _transform.GetMap(position);
        if (!TryComp<MapGridComponent>(map, out var gridComp))
            return false;

        if (plant.TryGetComponent<CEPlantComponent>(out var plantComp, _compFactory)
            && plantComp.SoilResourceGathering.Count > 0)
        {
            var tileRef = MapSystem.GetTileRef(map.Value, gridComp, position);
            var tile = Turf.GetContentTileDefinition(tileRef);

            if (!plantComp.SoilResourceGathering.ContainsKey(tile))
            {
                if (user is not null && _timing.IsFirstTimePredicted && _net.IsClient)
                {
                    _popup.PopupEntity(Loc.GetString("ce-farming-soil-wrong", ("seed", plant.Name)),
                        user.Value,
                        user.Value);
                }

                return false;
            }
        }

        if (IsTileOccupied((map.Value, gridComp), position, exclude))
        {
            if (user is not null && _timing.IsFirstTimePredicted && _net.IsClient)
                _popup.PopupEntity(Loc.GetString("ce-farming-soil-occupied"), user.Value, user.Value);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Mirrors <see cref="AnchorableSystem.TileFree(Entity{MapGridComponent}, Vector2i, int, int)"/> with MobMask,
    /// but supports exclude and treats any other plant as an obstacle. Puddles, decals and cables don't block planting.
    /// </summary>
    private bool IsTileOccupied(Entity<MapGridComponent> grid, EntityCoordinates position, EntityUid? exclude)
    {
        foreach (var anchored in MapSystem.GetAnchoredEntities(grid, position))
        {
            if (anchored == exclude)
                continue;

            if (PlantQuery.HasComp(anchored))
                return true;

            if (!_physicsQuery.TryGetComponent(anchored, out var body) || !body.CanCollide || !body.Hard)
                continue;

            if ((body.CollisionLayer & (int) CollisionGroup.MobMask) != 0)
                return true;
        }

        return false;
    }

    protected bool CanPlant(EntProtoId plant, EntityCoordinates position, EntityUid? user, EntityUid? exclude = null)
    {
        if (!_proto.Resolve(plant, out var plantProto))
            return false;

        return CanPlant(plantProto, position, user, exclude);
    }
}

[Serializable, NetSerializable]
public sealed partial class CEPlantSeedDoAfterEvent : DoAfterEvent
{
    [DataField(required:true)]
    public NetCoordinates Coordinates;

    public CEPlantSeedDoAfterEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }

    public override DoAfterEvent Clone() => this;
}

