using Content.Shared._CE.DoAfter;
using Content.Shared._CE.ZLevels.Core;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._CE.InteractionParticle;

/// <summary>
/// Decides when the "item above the head" particle plays: on picking an item into a hand and on starting a DoAfter with a used item.
/// The performer sees it through prediction, everyone else gets it from the server.
/// </summary>
public sealed partial class CEInteractionParticleSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    [SubscribeLocalEvent]
    private void OnDidEquipHand(Entity<HandsComponent> ent, ref DidEquipHandEvent args)
    {
        if (args.CEHandMove)
            return;

        // Client-side container inserts from server state are other players' pickups, the server sends those itself.
        if (_timing.ApplyingState)
            return;

        Play(ent, args.Equipped, false);
    }

    [SubscribeLocalEvent]
    private void OnDoAfterStarted(Entity<DoAfterComponent> ent, ref CEDoAfterStartedEvent args)
    {
        if (args.DoAfter.Args.Used is not { } used || used == ent.Owner)
            return;

        Play(ent, used, args.DoAfter.Args.Hidden);
    }

    private void Play(EntityUid performer, EntityUid item, bool hidden)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var ev = new CEInteractionParticleEvent(GetNetEntity(performer), GetNetEntity(item));

        if (_net.IsClient)
        {
            RaiseLocalEvent(ev);
            return;
        }

        if (hidden)
            return;

        RaiseNetworkEvent(ev, CEFilter.ZPvsExcept(performer, EntityManager));
    }
}
