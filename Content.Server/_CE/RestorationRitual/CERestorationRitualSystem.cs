using Content.Server._CE.GameTicking;
using Content.Server.Chat.Managers;
using Content.Shared._CE.EntityEffect;
using Content.Shared._CE.Murk.Components;
using Content.Shared._CE.RestorationRitual;
using Content.Shared._CE.RestorationRitual.Components;
using Content.Shared._CE.Trade.MainQuest;
using Content.Shared.Chat;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._CE.RestorationRitual;

/// <summary>
/// Locks lightheart shards into the ritual pedestals, throws duplicates back out, starts the Restoration Ritual
/// once every shard is accepted, and fails it when the exposed core breaks. The ritual's timer and the sphere's
/// states belong to <see cref="CEMurkConsumingRuleSystem"/>.
/// </summary>
public sealed partial class CERestorationRitualSystem : CESharedRestorationRitualSystem
{
    [Dependency] private CEMurkConsumingRuleSystem _rule = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly Color AnnouncementColor = Color.FromHex("#5FB7FF");

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CERitualPedestalComponent, ItemSlotsComponent>();
        while (query.MoveNext(out var uid, out var pedestal, out var slots))
        {
            if (pedestal.ImpactTime is not { } impact || _timing.CurTime < impact)
                continue;

            pedestal.ImpactTime = null;

            if (pedestal.Rejecting)
                RejectShard((uid, pedestal, slots));
            else
                AcceptShard((uid, pedestal));
        }
    }

    protected override string? GetServerInvalidReason(EntityUid target)
    {
        if (!TryCollectShards(_transform.GetMapId(target), out var missing))
            return Loc.GetString("ce-restoration-ritual-unavailable");

        if (missing.Count > 0)
            return Loc.GetString("ce-restoration-ritual-missing-shards", ("shards", string.Join(", ", missing)));

        return null;
    }

    /// <summary>
    /// Starts the ritual on <paramref name="target"/> if it's a cracked sphere and every shard is accepted on a
    /// pedestal. Tells <paramref name="user"/> why not otherwise.
    /// </summary>
    public bool TryStartRitual(EntityUid user, EntityUid target)
    {
        if ((GetSharedInvalidReason(target) ?? GetServerInvalidReason(target)) is { } reason)
        {
            Popup.PopupEntity(reason, user, user);
            return false;
        }

        return TryComp<CEMurkLusconSphereComponent>(target, out var sphere) && _rule.TryStartRitual((target, sphere));
    }

    /// <summary>
    /// Finds which of the indices 1..shard count aren't accepted on any ritual pedestal of the map.
    /// </summary>
    private bool TryCollectShards(MapId map, out List<int> missing)
    {
        missing = new List<int>();

        if (!_rule.TryGetShardCount(out var shardCount))
            return false;

        var present = new HashSet<int>();
        var query = EntityQueryEnumerator<CERitualPedestalComponent, TransformComponent>();
        while (query.MoveNext(out var pedestal, out var xform))
        {
            if (xform.MapID != map || !pedestal.Accepted || pedestal.ShardIndex is not { } index)
                continue;

            present.Add(index);
        }

        for (var i = 1; i <= shardCount; i++)
        {
            if (!present.Contains(i))
                missing.Add(i);
        }

        return true;
    }

    /// <summary>
    /// Whether a shard with <paramref name="index"/> is already accepted or pending on another pedestal of the map.
    /// </summary>
    private bool IsShardPlaced(EntityUid exclude, MapId map, int index)
    {
        var query = EntityQueryEnumerator<CERitualPedestalComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var pedestal, out var xform))
        {
            if (uid != exclude && xform.MapID == map && pedestal.ShardIndex == index)
                return true;
        }

        return false;
    }

    [SubscribeLocalEvent]
    private void OnShardInserted(Entity<CERitualPedestalComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.Slot || ent.Comp.ShardIndex != null || ent.Comp.Rejecting)
            return;

        if (!TryComp<CEQuestItemComponent>(args.Entity, out var questItem))
            return;

        _itemSlots.SetLock(ent.Owner, ent.Comp.Slot, true);

        ent.Comp.Rejecting = IsShardPlaced(ent, _transform.GetMapId(ent.Owner), questItem.Index);
        ent.Comp.ShardIndex = ent.Comp.Rejecting ? null : questItem.Index;
        ent.Comp.ImpactTime = _timing.CurTime + ent.Comp.ImpactDelay;

        if (!ent.Comp.Rejecting)
            ApplyEffects(ent, ent.Comp.InsertEffects);
    }

    private void AcceptShard(Entity<CERitualPedestalComponent> ent)
    {
        ent.Comp.Accepted = true;
        ApplyEffects(ent, ent.Comp.AcceptEffects);
        AnnounceShard(_transform.GetMapId(ent.Owner));
    }

    /// <summary>
    /// Tells everyone in chat which shard was just accepted and how many are still missing.
    /// </summary>
    private void AnnounceShard(MapId map)
    {
        if (!TryCollectShards(map, out var missing) || !_rule.TryGetShardCount(out var shardCount))
            return;

        var title = Loc.GetString("ce-ritual-pedestal-accepted-title", ("count", shardCount - missing.Count));
        var message = Loc.GetString("ce-ritual-pedestal-accepted-desc", ("remaining", missing.Count));
        var wrapped = Loc.GetString("ce-ritual-pedestal-accepted-wrap",
            ("title", FormattedMessage.EscapeText(title)),
            ("message", FormattedMessage.EscapeText(message)));

        _chat.ChatMessageToAll(ChatChannel.Radio, message, wrapped, default, false, true, AnnouncementColor);
    }

    private void RejectShard(Entity<CERitualPedestalComponent, ItemSlotsComponent> ent)
    {
        ent.Comp1.Rejecting = false;

        if (!_itemSlots.TryGetSlot((ent.Owner, ent.Comp2), ent.Comp1.Slot, out var slot))
            return;

        _itemSlots.SetLock((ent.Owner, ent.Comp2), slot, false);

        if (!_itemSlots.TryEject(ent.Owner, slot, null, out var shard))
            return;

        if (TryComp<PhysicsComponent>(shard, out var body))
            _physics.SetLinearVelocity(shard.Value, _random.NextAngle().ToVec() * ent.Comp1.RejectSpeed, body: body);

        ApplyEffects(ent, ent.Comp1.RejectEffects);
        Popup.PopupEntity(Loc.GetString("ce-ritual-pedestal-shard-rejected"), ent);
    }

    private void ApplyEffects(EntityUid pedestal, List<CEEntityEffect> effects)
    {
        var args = new CEEntityEffectArgs(EntityManager, pedestal, null, Angle.Zero, 0f, pedestal, Transform(pedestal).Coordinates);
        foreach (var effect in effects)
        {
            effect.Effect(args);
        }
    }

    [SubscribeLocalEvent]
    private void OnStartRitualEvent(Entity<CEMurkLusconSphereComponent> ent, ref CEStartRestorationRitualEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartRitual(args.User, ent);
    }

    [SubscribeLocalEvent]
    private void OnSphereBroken(Entity<CEMurkLusconSphereComponent> ent, ref BreakageEventArgs args)
    {
        _rule.FailRitual(ent);
    }
}
