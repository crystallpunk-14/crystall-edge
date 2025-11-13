using Content.Shared._CE.Farming.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CE.Farming;

public abstract partial class CESharedFarmingSystem
{
    private void InitializeSeeds()
    {
        SubscribeLocalEvent<CESeedComponent, AfterInteractEvent>(OnSeedInteract);

        SubscribeLocalEvent<CESeedComponent, CEPlantSeedDoAfterEvent>(OnSeedPlantedDoAfter);
        SubscribeLocalEvent<CESeedComponent, ExaminedEvent>(OnSeedExamine);
    }

    private void OnSeedExamine(Entity<CESeedComponent> ent, ref ExaminedEvent args)
    {
        if (!_proto.Resolve(ent.Comp.PlantProto, out var plantProto))
            return;

        args.PushMarkup(Loc.GetString("ce-farming-seed-examine", ("name", plantProto.Name)));
    }

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
            _stack.SetCount(ent, stack.Count - 1);
        else
            QueueDel(ent);
    }

    protected bool CanPlant(EntityPrototype plant, EntityCoordinates position, EntityUid? user, EntityUid? exclude = null)
    {
        var map = _transform.GetMap(position);
        if (!TryComp<MapGridComponent>(map, out var gridComp))
            return false;

        if (plant.TryGetComponent<CEPlantComponent>(out var plantComp, _compFactory)
            && plantComp.SoilTile.Count > 0)
        {
            var tileRef = _map.GetTileRef(map.Value, gridComp, position);
            var tile = _turf.GetContentTileDefinition(tileRef);

            if (!plantComp.SoilTile.Contains(tile))
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

        foreach (var anchored in _map.GetAnchoredEntities((map.Value, gridComp), position))
        {
            if (anchored == exclude)
                continue;

            if (user is not null && _timing.IsFirstTimePredicted && _net.IsClient) _popup.PopupEntity(Loc.GetString("ce-farming-soil-occupied"), user.Value, user.Value);
                return false;
        }

        return true;
    }

    protected bool CanPlant(EntProtoId plant, EntityCoordinates position, EntityUid? user, EntityUid? exclude = null)
    {
        if (!_proto.Resolve(plant, out var plantProto))
            return false;

        return CanPlant(plantProto, position, user);
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

