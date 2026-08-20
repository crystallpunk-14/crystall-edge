using Content.Shared._CE.MagicEssence.Components;
using Content.Shared._CE.Press.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._CE.MagicEssence.Systems;

/// <summary>
/// Handles CEThaumaturgySplitterComponent, a CEPressTargetComponent that converts crushed
/// entities with thaumaturgical essence into essence items instead of letting the press just
/// damage them.
/// </summary>
public sealed partial class CEThaumaturgySplitterSystem : EntitySystem
{
    [Dependency] private CEMagicEssenceSystem _essence = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEThaumaturgySplitterComponent, CEPressCrushingTargetEvent>(OnCrushingTarget);
    }

    private void OnCrushingTarget(Entity<CEThaumaturgySplitterComponent> ent, ref CEPressCrushingTargetEvent args)
    {
        var coords = Transform(ent).Coordinates;

        foreach (var crushedUid in new HashSet<EntityUid>(args.Entities))
        {
            var essences = _essence.GetEssence(crushedUid);
            if (essences.Count == 0)
                continue;

            if (_mobState.IsAlive(crushedUid))
                continue;

            // Deterministic on both client and server (GetEssence is synced), so both agree on
            // whether this entity was handled, even though the actual delete/spawn is server-only.
            args.Entities.Remove(crushedUid);

            if (!_net.IsServer)
                continue;

            QueueDel(crushedUid);

            foreach (var (type, amount) in essences)
            {
                if (amount <= 0 || !_proto.TryIndex(type, out var essenceType) || essenceType.EssenceProto is not { } essenceProto)
                    continue;

                for (var i = 0; i < amount; i++)
                {
                    var spawned = SpawnAtPosition(essenceProto, coords);
                    _throwing.TryThrow(spawned, _random.NextVector2(), ent.Comp.ScatterThrowSpeed, doSpin: true);
                }
            }
        }
    }
}
