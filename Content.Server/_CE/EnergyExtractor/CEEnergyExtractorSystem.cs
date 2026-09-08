using Content.Server.Destructible;
using Content.Server.Power.EntitySystems;
using Content.Shared._CE.EnergyExtractor;
using Content.Shared.Power.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._CE.EnergyExtractor;

/// <summary>
/// Once per <see cref="CEEnergyExtracterComponent.ProcessFrequency"/> takes one item out of the
/// extractor's internal storage, destroys it through <see cref="DestructibleSystem"/> and charges
/// the machine battery by that item's <see cref="CEEnergyExtractableComponent.Energy"/>.
/// </summary>
public sealed partial class CEEnergyExtractorSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private DestructibleSystem _destructible = default!;
    [Dependency] private BatterySystem _battery = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEEnergyExtracterComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out var extractor, out var battery))
        {
            if (_timing.CurTime < extractor.NextProcessTime)
                continue;

            if (!this.IsPowered(uid, EntityManager))
                continue;

            if (!_container.TryGetContainer(uid, extractor.ContainerId, out var container))
                continue;

            if (!TryGetNextFuel((uid, extractor), container, out var item, out var energy))
                continue;

            extractor.NextProcessTime = _timing.CurTime + extractor.ProcessFrequency;

            if (!_destructible.DestroyEntity(item))
                continue;

            if (energy > 0f)
                _battery.ChangeCharge((uid, battery), energy);

            _audio.PlayPvs(extractor.ProcessSound, uid);
        }
    }

    private bool TryGetNextFuel(
        Entity<CEEnergyExtracterComponent> ent,
        BaseContainer container,
        out EntityUid item,
        out float energy)
    {
        item = default;
        energy = 0f;

        foreach (var contained in container.ContainedEntities)
        {
            if (!TryComp<CEEnergyExtractableComponent>(contained, out var extractable))
                continue;

            if (!_whitelist.CheckBoth(contained, ent.Comp.Blacklist, ent.Comp.Whitelist))
                continue;

            item = contained;
            energy = extractable.Energy;
            return true;
        }

        return false;
    }
}
