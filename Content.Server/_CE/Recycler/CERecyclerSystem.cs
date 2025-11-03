using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.Recycler;
using Robust.Shared.Physics.Events;

namespace Content.Server._CE.Recycler;

/// <inheritdoc/>
public sealed class CERecyclerSystem : CESharedRecyclerSystem
{
    private EntityQuery<PowerConsumerComponent> _powerQuery;
    public override void Initialize()
    {
        base.Initialize();

        _powerQuery = GetEntityQuery<PowerConsumerComponent>();

        SubscribeLocalEvent<Shared._CE.Recycler.CERecyclerComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(Entity<Shared._CE.Recycler.CERecyclerComponent> ent, ref StartCollideEvent args)
    {
        if (!_powerQuery.TryComp(ent, out var consumer))
            return;

        if (consumer.ReceivedPower < consumer.DrawRate)
            return;

        Recycle(ent, args.OtherEntity);
    }

    private void Recycle(Entity<Shared._CE.Recycler.CERecyclerComponent> ent, EntityUid other)
    {
        QueueDel(other);
    }
}
