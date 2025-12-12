using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Actions.Spells;

public sealed partial class CESpellSpawnEntityOnTarget : CESpellEffect
{
    [DataField]
    public List<EntProtoId> Spawns = new();

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        EntityCoordinates? targetPoint = null;
        if (args.Position is not null)
            targetPoint = args.Position.Value;
        if (args.Target is not null && entManager.TryGetComponent<TransformComponent>(args.Target.Value, out var transformComponent))
            targetPoint = transformComponent.Coordinates;

        if (targetPoint is null)
            return;

        var netMan = IoCManager.Resolve<INetManager>();
        if (netMan.IsClient)
            return;

        foreach (var spawn in Spawns)
        {
            entManager.SpawnAtPosition(spawn, targetPoint.Value);
        }
    }
}
