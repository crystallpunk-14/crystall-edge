using Content.Server._CE.GameTicking;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.RestorationRitual;
using Content.Shared._CE.RestorationRitual.Components;
using Content.Shared._CE.Trade.MainQuest;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Robust.Shared.Map;

namespace Content.Server._CE.RestorationRitual;

/// <summary>
/// Starts the Restoration Ritual once the shards are on the ritual pedestals, and fails it when the
/// exposed core breaks. The ritual's timer and the sphere's states belong to <see cref="CEMurkConsumingRuleSystem"/>.
/// </summary>
public sealed partial class CERestorationRitualSystem : CESharedRestorationRitualSystem
{
    [Dependency] private CEMurkConsumingRuleSystem _rule = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override string? GetServerInvalidReason(EntityUid target)
    {
        if (!TryCollectShards(_transform.GetMapId(target), out var missing, out _))
            return Loc.GetString("ce-restoration-ritual-unavailable");

        if (missing.Count > 0)
            return Loc.GetString("ce-restoration-ritual-missing-shards", ("shards", string.Join(", ", missing)));

        return null;
    }

    /// <summary>
    /// Starts the ritual on <paramref name="target"/> if it's a cracked sphere and every shard is on a
    /// pedestal; the shards are consumed. Tells <paramref name="user"/> why not otherwise.
    /// </summary>
    public bool TryStartRitual(EntityUid user, EntityUid target)
    {
        if ((GetSharedInvalidReason(target) ?? GetServerInvalidReason(target)) is { } reason)
        {
            Popup.PopupEntity(reason, user, user);
            return false;
        }

        if (!TryComp<CEMurkLusconSphereComponent>(target, out var sphere) ||
            !TryCollectShards(_transform.GetMapId(target), out _, out var shards) ||
            !_rule.TryStartRitual((target, sphere)))
            return false;

        foreach (var shard in shards)
        {
            QueueDel(shard);
        }

        return true;
    }

    /// <summary>
    /// Finds the shards on every ritual pedestal of the map and which of the indices 1..shard count are missing.
    /// Extra pedestals and duplicate shards don't matter.
    /// </summary>
    private bool TryCollectShards(MapId map, out List<int> missing, out List<EntityUid> shards)
    {
        missing = new List<int>();
        shards = new List<EntityUid>();

        if (!_rule.TryGetShardCount(out var shardCount))
            return false;

        var present = new HashSet<int>();
        var query = EntityQueryEnumerator<CERitualPedestalComponent, ItemSlotsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var pedestal, out var slots, out var xform))
        {
            if (xform.MapID != map)
                continue;

            if (!_itemSlots.TryGetSlot(uid, pedestal.Slot, out var slot, slots) || slot.Item is not { } item)
                continue;

            if (!TryComp<CEQuestItemComponent>(item, out var questItem))
                continue;

            present.Add(questItem.Index);
            shards.Add(item);
        }

        for (var i = 1; i <= shardCount; i++)
        {
            if (!present.Contains(i))
                missing.Add(i);
        }

        return true;
    }

    [SubscribeLocalEvent]
    private void OnSphereBroken(Entity<CEMurkLusconSphereComponent> ent, ref BreakageEventArgs args)
    {
        _rule.FailRitual(ent);
    }
}
