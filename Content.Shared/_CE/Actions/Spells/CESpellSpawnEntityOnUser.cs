using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Actions.Spells;

public sealed partial class CESpellSpawnEntityOnUser : CESpellEffect
{
    [DataField]
    public List<EntProtoId> Spawns = new();

    public override void Effect(EntityManager entManager, CESpellEffectBaseArgs args)
    {
        if (args.User is null || !entManager.TryGetComponent<TransformComponent>(args.User.Value, out var transformComponent))
            return;

        var netMan = IoCManager.Resolve<INetManager>();
        if (netMan.IsClient)
            return;

        foreach (var spawn in Spawns)
        {
            entManager.SpawnAtPosition(spawn, transformComponent.Coordinates);
        }
    }
}
