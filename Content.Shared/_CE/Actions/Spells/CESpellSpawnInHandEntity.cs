using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Actions.Spells;

public sealed partial class CESpellSpawnInHandEntity : CESpellEffect
{
    [DataField]
    public List<EntProtoId> Spawns = new();

    [DataField]
    public bool DeleteIfCantPickup = false;

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.Target is null)
            return;

        if (!entManager.TryGetComponent<TransformComponent>(args.Target.Value, out var transformComponent))
            return;

        var handSystem = entManager.System<SharedHandsSystem>();

        foreach (var spawn in Spawns)
        {
            var item = entManager.PredictedSpawnAtPosition(spawn, transformComponent.Coordinates);
            if (!handSystem.TryPickupAnyHand(args.Target.Value, item) && DeleteIfCantPickup)
                entManager.QueueDeleteEntity(item);
        }
    }
}
