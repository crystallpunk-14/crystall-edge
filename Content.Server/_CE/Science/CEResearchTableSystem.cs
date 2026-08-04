using Content.Server._CE.Science.Components;
using Content.Shared._CE.MagicEssence.Prototypes;
using Content.Shared._CE.MagicEssence.Systems;
using Content.Shared._CE.Science.Components;
using Content.Shared._CE.Science;
using Content.Shared._CE.Science.Prototypes;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._CE.Science;

public sealed partial class CEResearchTableSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private UserInterfaceSystem _userInterface = default!;
    [Dependency] private CEScienceSystem _science = default!;
    [Dependency] private CEMagicEssenceSystem _essence = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly SoundSpecifier DiscoverySound =
        new SoundPathSpecifier(new ResPath("/Audio/_CE/Effects/knowledge_learned.ogg"));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEResearchTableComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<CEResearchTableComponent, CEResearchTableChooseDiscoveryMessage>(OnChooseDiscovery);
        SubscribeLocalEvent<CEResearchTableComponent, CEResearchTableMergeEssenceMessage>(OnMergeEssence);
    }

    private void OnBeforeUIOpen(Entity<CEResearchTableComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        SendState(ent, args.User);
    }

    private void OnChooseDiscovery(Entity<CEResearchTableComponent> ent, ref CEResearchTableChooseDiscoveryMessage args)
    {
        var data = EnsureComp<CEScienceResearchDataComponent>(args.Actor);

        // Only ever operate on coordinates the player can already see.
        if (!data.Researched.TryGetValue(args.Area, out var researched) || !researched.Contains(args.Coordinate))
            return;

        if (!_science.ResolveChoice(args.Area, args.Coordinate, args.Discovery, args.Actor))
            return;

        _audio.PlayPvs(DiscoverySound, ent);

        BroadcastTileUpdate(args.Area, args.Coordinate);
    }

    /// <summary>
    /// Merges 2 selected aspects into the higher-tier one they combine into, per
    /// <see cref="CEMagicEssenceSystem.TryGetMergeResult"/>. Re-validates the recipe and the actor's
    /// points here rather than trusting the client, which only used the same check to decide whether
    /// to enable the merge button. Points are <see cref="Robust.Shared.GameStates.AutoNetworkedField"/>,
    /// so the client picks up the change through ordinary component state sync - no state to resend.
    /// </summary>
    private void OnMergeEssence(Entity<CEResearchTableComponent> ent, ref CEResearchTableMergeEssenceMessage args)
    {
        if (!_essence.TryGetMergeResult(args.First, args.Second, out var result))
            return;

        var data = EnsureComp<CEScienceResearchDataComponent>(args.Actor);

        // TryGetMergeResult never matches First == Second (no recipe combines an aspect with itself),
        // so this is always exactly 1 of each.
        var cost = new Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int>
        {
            [args.First] = 1,
            [args.Second] = 1,
        };

        if (!_science.TrySpendPoints((args.Actor, data), cost))
            return;

        _science.GrantPoints((args.Actor, data), new Dictionary<ProtoId<CEMagicEssenceTypePrototype>, int> { [result] = 1 });
    }

    /// <summary>
    /// Refreshes the research table state for every player, across every research table, who
    /// currently has <paramref name="coordinate"/> researched in <paramref name="area"/> - not just
    /// the player whose action caused the change. Needed since the shared, round-wide map tile at
    /// that coordinate may have just changed for everyone (e.g. a star got opened or resolved).
    /// </summary>
    private void BroadcastTileUpdate(ProtoId<CEScienceAreaPrototype> area, Vector2i coordinate)
    {
        var query = EntityQueryEnumerator<CEResearchTableComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            foreach (var actor in _userInterface.GetActors((uid, null), CEResearchTableUiKey.Key))
            {
                if (!TryComp<CEScienceResearchDataComponent>(actor, out var data))
                    continue;

                if (!data.Researched.TryGetValue(area, out var researched) || !researched.Contains(coordinate))
                    continue;

                SendState(uid, actor);
                break;
            }
        }
    }

    /// <summary>
    /// Pushes each area's tile content, filtered down to only what <paramref name="actor"/> has
    /// personally researched - that Researched set is data the actor's own client already knows
    /// (it lives on their own networked <see cref="CEScienceResearchDataComponent"/>), so filtering
    /// by it here doesn't hand out anything new. The raw Researched set and the actor's points are
    /// never put in this (shared, per-table) state - those are read by the client locally off its
    /// own component instead.
    /// </summary>
    private void SendState(EntityUid uid, EntityUid actor)
    {
        if (!_science.TryGetSingleton(out var science))
            return;

        var data = EnsureComp<CEScienceResearchDataComponent>(actor);

        var areas = new Dictionary<ProtoId<CEScienceAreaPrototype>, CEResearchTableAreaData>();
        foreach (var area in _proto.EnumeratePrototypes<CEScienceAreaPrototype>())
        {
            var tiles = new Dictionary<Vector2i, CEScienceMapTile>();

            if (data.Researched.TryGetValue(area.ID, out var researched)
                && science.Areas.TryGetValue(area.ID, out var areaTiles))
            {
                foreach (var coordinate in researched)
                {
                    if (areaTiles.TryGetValue(coordinate, out var tile))
                        tiles[coordinate] = tile;
                }
            }

            areas[area.ID] = new CEResearchTableAreaData(tiles);
        }

        _userInterface.SetUiState(uid, CEResearchTableUiKey.Key, new CEResearchTableState(areas));
    }
}
