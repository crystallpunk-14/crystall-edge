using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Actions.Spells;

public sealed partial class CESpellSpawnEntitiesOnTargetInRadius : CESpellEffect
{
    [DataField]
    public EntProtoId Spawn = new();

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

        // Spawn in center
        entManager.SpawnAtPosition(Spawn, targetPoint.Value);

        //Spawn in other directions
        for (var i = 0; i < 4; i++)
        {
            var direction = (DirectionFlag) (1 << i);
            var coords = targetPoint.Value.Offset(direction.AsDir().ToVec());

            entManager.SpawnAtPosition(Spawn, coords);
        }
    }
}
