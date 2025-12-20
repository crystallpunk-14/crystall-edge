using Content.Shared._CE.Thief;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Thief;

public sealed partial class CEClientThiefSystem : EntitySystem
{
    private EntProtoId _vfx = "CETreasureSparkVFXThiefSound";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActorComponent, CEThiefShowTreasuresEvent>(OnShowTreasures);
    }

    private void OnShowTreasures(Entity<ActorComponent> ent, ref CEThiefShowTreasuresEvent args)
    {
        var query = EntityQueryEnumerator<CETheftValueComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var theftValue, out var transform))
        {
            SpawnAtPosition(_vfx, transform.Coordinates);
        }
    }
}
