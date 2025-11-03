using Content.Server.Power.Components;
using Content.Shared._CE.Recycler;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Server._CE.Recycler;

/// <inheritdoc/>
public sealed class CERecyclerSystem : CESharedRecyclerSystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    private EntityQuery<PowerConsumerComponent> _powerQuery;
    public override void Initialize()
    {
        base.Initialize();

        _powerQuery = GetEntityQuery<PowerConsumerComponent>();

        SubscribeLocalEvent<CERecyclerComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(Entity<CERecyclerComponent> ent, ref StartCollideEvent args)
    {
        if (!_powerQuery.TryComp(ent, out var consumer))
            return;
        if (consumer.ReceivedPower < consumer.DrawRate)
            return;
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        Recycle(ent, args.OtherEntity);
    }

    private void Recycle(Entity<CERecyclerComponent> ent, EntityUid other)
    {
        if (TryComp<BodyComponent>(other, out var bodyComp))
        {
            _body.GibBody(other, true, bodyComp);
            return;
        }
        QueueDel(other);
    }
}
