using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Server._CE.Statistics;

/// <summary>
/// Buffers per-player statistics during the round and saves them to the database at round end.
/// Achievements react to <see cref="CEStatisticUpdatedEvent"/> raised after the save.
/// Disabled when <see cref="CCVars.DatabaseSynchronous"/> is set (integration tests).
/// </summary>
public sealed partial class CEStatisticSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    public void AddStatistic(NetUserId player, CEStatisticDef stat, long amount = 1)
    {
        AddToBuffer(player, stat.Key, amount);
    }

    public void AddStatistic<T>(NetUserId player, CEStatisticDef<T> stat, T arg, long amount = 1) where T : notnull
    {
        AddToBuffer(player, stat.GetKey(arg), amount);
    }

    /// <summary>
    /// Adds the stat to the player controlling <paramref name="entity"/>. Returns false if no player controls it.
    /// </summary>
    public bool TryAddStatistic(EntityUid entity, CEStatisticDef stat, long amount = 1)
    {
        if (!TryGetPlayer(entity, out var player))
            return false;

        AddStatistic(player, stat, amount);
        return true;
    }

    public bool TryAddStatistic<T>(EntityUid entity, CEStatisticDef<T> stat, T arg, long amount = 1) where T : notnull
    {
        if (!TryGetPlayer(entity, out var player))
            return false;

        AddStatistic(player, stat, arg, amount);
        return true;
    }

    /// <summary>
    /// Value the player gained this round.
    /// </summary>
    public long GetRoundValue(NetUserId player, CEStatisticDef stat)
    {
        return GetBufferedValue(player, stat.Key);
    }

    public long GetRoundValue<T>(NetUserId player, CEStatisticDef<T> stat, T arg) where T : notnull
    {
        return GetBufferedValue(player, stat.GetKey(arg));
    }

    private bool TryGetPlayer(EntityUid entity, out NetUserId player)
    {
        player = default;
        if (!_mind.TryGetMind(entity, out _, out var mind) || mind.UserId is not { } userId)
            return false;

        player = userId;
        return true;
    }

    private void AddToBuffer(NetUserId player, string key, long amount)
    {
        if (_cfg.GetCVar(CCVars.DatabaseSynchronous))
            return;

        var buffer = EnsureBuffer();
        if (buffer.Comp.Closed)
            return;

        if (!buffer.Comp.Values.TryGetValue(player, out var values))
        {
            values = new Dictionary<string, long>();
            buffer.Comp.Values[player] = values;
        }

        values[key] = values.GetValueOrDefault(key) + amount;
    }

    private long GetBufferedValue(NetUserId player, string key)
    {
        if (!TryGetBuffer(out var buffer) || !buffer.Value.Comp.Values.TryGetValue(player, out var values))
            return 0;

        return values.GetValueOrDefault(key);
    }

    private bool TryGetBuffer([NotNullWhen(true)] out Entity<CEStatisticsBufferComponent>? buffer)
    {
        var query = EntityQueryEnumerator<CEStatisticsBufferComponent>();
        if (query.MoveNext(out var uid, out var comp))
        {
            buffer = (uid, comp);
            return true;
        }

        buffer = null;
        return false;
    }

    private Entity<CEStatisticsBufferComponent> EnsureBuffer()
    {
        if (TryGetBuffer(out var buffer))
            return buffer.Value;

        var uid = Spawn(null, MapCoordinates.Nullspace);
        return (uid, AddComp<CEStatisticsBufferComponent>(uid));
    }

    [SubscribeLocalEvent]
    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        if (_cfg.GetCVar(CCVars.DatabaseSynchronous))
            return;

        var buffer = EnsureBuffer();
        if (buffer.Comp.Closed)
            return;

        RaiseLocalEvent(new CEStatisticsCollectEvent());
        buffer.Comp.Closed = true;

        if (buffer.Comp.Values.Count == 0)
            return;

        var roundValues = new Dictionary<Guid, Dictionary<string, long>>();
        foreach (var (player, values) in buffer.Comp.Values)
        {
            roundValues[player.UserId] = new Dictionary<string, long>(values);
        }

        _ = SaveToDatabaseAsync(roundValues);
    }

    private async Task SaveToDatabaseAsync(Dictionary<Guid, Dictionary<string, long>> roundValues)
    {
        Dictionary<Guid, Dictionary<string, long>> totalValues;
        try
        {
            totalValues = await _db.AddPlayerStatistics(roundValues);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save round statistics: {e}");
            return;
        }

        foreach (var (player, values) in roundValues)
        {
            var playerTotals = totalValues.GetValueOrDefault(player) ?? new Dictionary<string, long>();
            RaiseLocalEvent(new CEStatisticUpdatedEvent(new NetUserId(player), values, playerTotals));
        }
    }
}
