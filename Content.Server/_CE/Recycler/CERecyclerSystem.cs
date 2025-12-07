using Content.Server.Audio;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.Recycler;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Destructible;
using Content.Shared.Materials;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Server._CE.Recycler;

/// <inheritdoc/>
public sealed class CERecyclerSystem : CESharedRecyclerSystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MaterialStorageSystem _material = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<PowerConsumerComponent> _powerQuery;

    public override void Initialize()
    {
        base.Initialize();

        _powerQuery = GetEntityQuery<PowerConsumerComponent>();

        SubscribeLocalEvent<CERecyclerComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<CERecyclerComponent, PowerConsumerReceivedChanged>(OnPowerChanged);
    }

    private void OnPowerChanged(Entity<CERecyclerComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        _ambient.SetAmbience(ent,  args.ReceivedPower >= args.DrawRate);
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
        if (!_whitelist.CheckBoth(other, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;
        if (!TryComp<MaterialStorageComponent>(ent, out var materialStorage))
            return;

        var xform = Transform(ent);
        _audio.PlayPvs(ent.Comp.RecycleSound, xform.Coordinates);
        if (TryComp<BodyComponent>(other, out var bodyComp))
        {
            _body.GibBody(other, true, bodyComp);
            return;
        }

        var spawnPos =
            xform.Coordinates.Offset(xform.LocalRotation.ToWorldVec().Normalized() * ent.Comp.SpawnOffset);

        if (TryComp<PhysicalCompositionComponent>(other, out var physComp))
        {
            if (TryComp<StackComponent>(other, out var stack))
            {
                var count = stack.Count;
                Dictionary<string,int> materialComposition = new();
                foreach (var (s, value) in physComp.MaterialComposition)
                {
                    materialComposition[s] = value * count;
                }
                _material.TryChangeMaterialAmount((ent.Owner, materialStorage), materialComposition);
            }
            else
                _material.TryChangeMaterialAmount((ent.Owner, materialStorage), physComp.MaterialComposition);

            _material.EjectAllMaterial(ent.Owner, spawnPos, materialStorage);
        }

        _destructible.DestroyEntity(other);
        _transform.SetCoordinates(other, spawnPos); //To prevent double reclaiming
    }
}
