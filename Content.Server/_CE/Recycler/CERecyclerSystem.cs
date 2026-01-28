using Content.Server.Audio;
using Content.Server.Destructible;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.Recycler;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Materials;
using Content.Shared.Power;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Server._CE.Recycler;

/// <inheritdoc/>
public sealed class CERecyclerSystem : CESharedRecyclerSystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MaterialStorageSystem _material = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CERecyclerComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<CERecyclerComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnPowerChanged(Entity<CERecyclerComponent> ent, ref PowerChangedEvent args)
    {
        _ambient.SetAmbience(ent,  args.Powered);
    }

    private void OnCollide(Entity<CERecyclerComponent> ent, ref StartCollideEvent args)
    {
        if (!this.IsPowered(ent.Owner, EntityManager))
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
        _damageable.TryChangeDamage(other, ent.Comp.Damage);

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
            _destructible.DestroyEntity(other);
        }

        _transform.SetCoordinates(other, spawnPos); //To prevent double reclaiming
    }
}
