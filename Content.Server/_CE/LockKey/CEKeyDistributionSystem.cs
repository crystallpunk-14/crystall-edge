using Content.Shared._CE.LockKey;
using Content.Shared._CE.LockKey.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CE.LockKey;

public sealed partial class CEKeyDistributionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly CEKeyholeGenerationSystem _keyGeneration = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEAbstractKeyComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<CEAbstractKeyComponent> ent, ref MapInitEvent args)
    {
        if (!TrySetShape(ent) && ent.Comp.DeleteOnFailure)
            QueueDel(ent);
    }

    private bool TrySetShape(Entity<CEAbstractKeyComponent> ent)
    {
        var grid = Transform(ent).GridUid;

        if (grid is null)
            return false;

        if (!TryComp<CEKeyComponent>(ent, out var key))
            return false;

        if (!TryComp<StationMemberComponent>(grid.Value, out var member))
            return false;

        if (!TryComp<CEStationKeyDistributionComponent>(member.Station, out var distribution))
            return false;

        var keysList = new List<ProtoId<CELockTypePrototype>>(distribution.Keys);
        while (keysList.Count > 0)
        {
            var randomIndex = _random.Next(keysList.Count);
            var keyA = keysList[randomIndex];

            var indexedKey = _proto.Index(keyA);

            if (indexedKey.Group != ent.Comp.Group)
            {
                keysList.RemoveAt(randomIndex);
                continue;
            }

            _keyGeneration.SetShape((ent, key), indexedKey);
            distribution.Keys.Remove(indexedKey);
            return true;
        }

        return false;
    }
}
