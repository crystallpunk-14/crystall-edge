using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Actions.Spells;

public sealed partial class CESpellSpawnEntityOnUser : CESpellEffect
{
    [DataField]
    public List<EntProtoId> Spawns = new();

    [DataField]
    public bool Clientside = false;

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.User is null || !entManager.TryGetComponent<TransformComponent>(args.User.Value, out var transformComponent))
            return;

        var netMan = IoCManager.Resolve<INetManager>();

        foreach (var spawn in Spawns)
        {
            if (Clientside)
            {
                if (!netMan.IsClient)
                    continue;

                entManager.SpawnAtPosition(spawn, transformComponent.Coordinates);
            }
            else
            {
                entManager.PredictedSpawnAtPosition(spawn, transformComponent.Coordinates);
            }
        }
    }
}
