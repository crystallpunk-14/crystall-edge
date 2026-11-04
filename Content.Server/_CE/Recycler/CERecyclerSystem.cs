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
using Robust.Shared.Analyzers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;

namespace Content.Server._CE.Recycler;

/// <inheritdoc/>
public sealed partial class CERecyclerSystem : CESharedRecyclerSystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private MaterialStorageSystem _material = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private AmbientSoundSystem _ambient = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private DestructibleSystem _destructible = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CERecyclerComponent> ent, ref PowerChangedEvent args)
    {
        _ambient.SetAmbience(ent,  args.Powered);
    }

    [SubscribeLocalEvent]
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
        // An already recycled entity lives until the end of the tick and can hit the next recycler in a chain
        if (TerminatingOrDeleted(other) || EntityManager.IsQueuedForDeletion(other))
            return;
        if (!_whitelist.CheckBoth(other, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;
        if (!TryComp<MaterialStorageComponent>(ent, out var materialStorage))
            return;

        var xform = Transform(ent);
        _audio.PlayPvs(ent.Comp.RecycleSound, xform.Coordinates);
        _damageable.TryChangeDamage(other, ent.Comp.Damage);

        // Eject on the side opposite to where the item came from, so it can't loop back on a conveyor
        var ourPos = _transform.GetWorldPosition(xform);
        var passDir = ourPos - _transform.GetWorldPosition(other);
        if (passDir.LengthSquared() < 0.0001f)
            passDir = -_transform.GetWorldRotation(xform).ToWorldVec();

        var spawnPos = _transform.ToCoordinates(
            new MapCoordinates(ourPos + passDir.Normalized() * MathF.Abs(ent.Comp.SpawnOffset), xform.MapID));

        if (TryComp<PhysicalCompositionComponent>(other, out var physComp))
        {
            var count = CompOrNull<StackComponent>(other)?.Count ?? 1;
            var materialComposition = new Dictionary<string, int>();
            foreach (var (s, value) in physComp.MaterialComposition)
            {
                materialComposition[s] = value * count;
            }

            // Materials are granted only for an entity that is actually destroyed
            if (_destructible.DestroyEntity(other))
            {
                _material.TryChangeMaterialAmount((ent.Owner, materialStorage), materialComposition);
                _material.EjectAllMaterial(ent.Owner, spawnPos, materialStorage);
                return;
            }
        }

        _transform.SetCoordinates(other, spawnPos); //To prevent double reclaiming
    }
}
