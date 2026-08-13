using System.Linq;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.EntityEffect.Effects;

public sealed partial class SpawnInHandEntity : CEEntityEffectBase<SpawnInHandEntity>
{
    [DataField]
    public List<EntProtoId> Spawns = new();

    [DataField]
    public bool DeleteIfCantPickup;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (Spawns.Count == 0)
            return base.EntityEffectGuidebookText(prototype, entSys);

        var names = Spawns.Select(s => prototype.TryIndex(s, out var proto) ? proto.Name : s.Id);
        return Loc.GetString("ce-entity-effect-guidebook-spawn-in-hand", ("entities", string.Join(", ", names)));
    }
}

public sealed partial class CESpawnInHandEntityEffectSystem : CEEntityEffectSystem<SpawnInHandEntity>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    protected override void Effect(ref CEEntityEffectEvent<SpawnInHandEntity> args)
    {
        if (ResolveEffectEntity(args.Args, args.Effect.EffectTarget) is not { } entity)
            return;

        var transformComponent = Transform(entity);

        if (_net.IsClient)
            return;

        foreach (var spawn in args.Effect.Spawns)
        {
            var item = SpawnAtPosition(spawn, transformComponent.Coordinates);
            if (!_hands.TryPickupAnyHand(entity, item) && args.Effect.DeleteIfCantPickup)
                QueueDel(item);
        }
    }
}
