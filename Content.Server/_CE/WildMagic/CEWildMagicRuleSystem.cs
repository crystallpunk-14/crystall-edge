using Content.Server._CE.WildMagic.Components;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;

namespace Content.Server._CE.WildMagic;

/// <inheritdoc cref="CEWildMagicRuleComponent"/>
public sealed partial class CEWildMagicRuleSystem : GameRuleSystem<CEWildMagicRuleComponent>
{
    [Dependency] private CEWildMagicSystem _wildMagic = default!;

    /// <summary>
    /// Set while <see cref="ReconcileNodeCount"/> is deliberately deleting excess mandatory nodes,
    /// so <see cref="OnMandatoryNodeShutdown"/> knows not to replace them.
    /// </summary>
    private bool _trimmingNodes;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CEWildMagicMandatoryNodeComponent, ComponentShutdown>(OnMandatoryNodeShutdown);
    }

    /// <summary>
    /// By the time a game rule Starts (as opposed to just being Added), the round-start station and
    /// its z-map network are already fully built - unlike StationPostInitEvent, which can fire
    /// before CEZLevelsSystem has finished setting the network up.
    /// </summary>
    protected override void Started(EntityUid uid, CEWildMagicRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        ReconcileNodeCount(component.NodeCount);
    }

    private void OnMandatoryNodeShutdown(Entity<CEWildMagicMandatoryNodeComponent> ent, ref ComponentShutdown args)
    {
        if (_trimmingNodes)
            return;

        // Only keep replenishing the pool while a wild magic rule is still active - once it ends,
        // removed nodes just stay gone.
        if (!QueryActiveRules().MoveNext(out _, out _, out _, out _))
            return;

        SpawnMandatoryNode();
    }

    /// <summary>
    /// Brings the number of alive mandatory nodes up to (or down to) <paramref name="target"/>,
    /// spawning replacements or deleting the excess as needed.
    /// </summary>
    private void ReconcileNodeCount(int target)
    {
        var current = new List<EntityUid>();
        var query = EntityQueryEnumerator<CEWildMagicMandatoryNodeComponent>();
        while (query.MoveNext(out var uid, out _))
            current.Add(uid);

        if (current.Count < target)
        {
            for (var i = current.Count; i < target; i++)
                SpawnMandatoryNode();

            return;
        }

        if (current.Count == target)
            return;

        _trimmingNodes = true;
        for (var i = target; i < current.Count; i++)
            Del(current[i]);
        _trimmingNodes = false;
    }

    /// <summary>
    /// Spawns a wild magic node on the station, marked as mandatory - while this rule is active,
    /// removing it causes a replacement to be generated the same way.
    /// </summary>
    private EntityUid? SpawnMandatoryNode()
    {
        if (!_wildMagic.TryGetStationNetwork(out var network))
        {
            Log.Warning("CEWildMagicRuleSystem: couldn't resolve a station z-map network - no mandatory node spawned.");
            return null;
        }

        if (_wildMagic.SpawnNodeInNetwork(network) is not { } uid)
        {
            Log.Warning($"CEWildMagicRuleSystem: couldn't find a valid tile in z-map network {ToPrettyString(network.Owner)} - no mandatory node spawned.");
            return null;
        }

        EnsureComp<CEWildMagicMandatoryNodeComponent>(uid);
        return uid;
    }
}
