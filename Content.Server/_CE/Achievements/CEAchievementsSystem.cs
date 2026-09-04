using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._CE.Achievements;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._CE.Achievements;

/// <summary>
/// Maintains cached achievement percentage statistics, refreshing them on first player join
/// and when achievements are added or removed.
/// </summary>
public sealed partial class CEAchievementsSystem : EntitySystem
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private IServerNetManager _netManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    private Dictionary<string, float> _cachedPercentages = new();

    // Cache of currently-connected players' achievement sets. Keyed by player Guid.
    private readonly Dictionary<Guid, HashSet<string>> _playerAchievementsCache = new();
    private bool _initialLoad;

    public override void Initialize()
    {
        base.Initialize();

        _netManager.RegisterNetMessage<CEMsgAllAchievementsData>();
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    public bool HasCachedAchievement(Guid player, string achievementProtoId)
    {
        return _playerAchievementsCache.TryGetValue(player, out var achievements)
               && achievements.Contains(achievementProtoId);
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected)
        {
            // Remove the user from the cache on disconnect
            _playerAchievementsCache.Remove(args.Session.UserId);
            return;
        }

        if (args.NewStatus != SessionStatus.InGame)
            return;

        var userId = args.Session.UserId;

        if (!_initialLoad)
        {
            _initialLoad = true;
            await RefreshCachedPercentagesAsync();
        }

        try
        {
            // Try to use cached data if available, otherwise load from DB and cache it.
            if (!_playerAchievementsCache.TryGetValue(userId, out var playerAchievements))
            {
                var loaded = await _db.GetPlayerAchievements(userId);
                playerAchievements = new HashSet<string>(loaded);
                _playerAchievementsCache[userId] = playerAchievements;
            }

            var msg = new CEMsgAllAchievementsData
            {
                PlayerAchievements = new HashSet<string>(playerAchievements),
                AchievementPercentages = _cachedPercentages,
            };

            _netManager.ServerSendMessage(msg, args.Session.Channel);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to send achievements to {args.Session.Name}: {e}");
        }
    }

    private async Task RefreshCachedPercentagesAsync()
    {
        try
        {
            _cachedPercentages = await _db.GetAchievementPercentages();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to refresh achievement percentages: {e}");
        }
    }

    /// <summary>
    /// Adds an achievement to a player in the database, updates local caches and notifies connected clients.
    /// Returns true if the achievement was added, false if the player already had it.
    /// </summary>
    public async Task<bool> AddPlayerAchievementAsync(Guid player, string achievementProtoId)
    {
        try
        {
            var has = await _db.HasPlayerAchievement(player, achievementProtoId);
            if (has)
                return false;

            await _db.AddPlayerAchievement(player, achievementProtoId);

            // Refresh global percentages so the notification uses the new value
            await RefreshCachedPercentagesAsync();

            // Update cached player set if present
            if (!_playerAchievementsCache.TryGetValue(player, out var set))
            {
                set = new HashSet<string>();
                _playerAchievementsCache[player] = set;
            }

            set.Add(achievementProtoId);

            // Send achievement unlocked notification to connected sessions for this user
            foreach (var session in _playerManager.Sessions)
            {
                if (session.UserId != player)
                    continue;

                var ev = new CEAchievementUnlockedEvent(achievementProtoId,
                    _cachedPercentages.GetValueOrDefault(achievementProtoId, 0f));

                RaiseNetworkEvent(ev, session);
            }

            // Notify connected sessions for this user with updated achievement list
            foreach (var session in _playerManager.Sessions)
            {
                if (session.UserId != player)
                    continue;

                var msg = new CEMsgAllAchievementsData
                {
                    PlayerAchievements = new HashSet<string>(set),
                    AchievementPercentages = _cachedPercentages,
                };

                _netManager.ServerSendMessage(msg, session.Channel);
            }

            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to add achievement {achievementProtoId} to {player}: {e}");
            return false;
        }
    }

    /// <summary>
    /// Removes an achievement from a player in the database, updates local caches and notifies connected clients.
    /// Returns true if removed, false if player did not have the achievement.
    /// </summary>
    public async Task<bool> RemovePlayerAchievementAsync(Guid player, string achievementProtoId)
    {
        try
        {
            var has = await _db.HasPlayerAchievement(player, achievementProtoId);
            if (!has)
                return false;

            var removed = await _db.RemovePlayerAchievement(player, achievementProtoId);

            // Refresh global percentages after removal so clients get updated stats
            await RefreshCachedPercentagesAsync();

            // Update cached player set if present
            if (_playerAchievementsCache.TryGetValue(player, out var set))
            {
                set.Remove(achievementProtoId);
            }

            // Notify connected sessions for this user
            foreach (var session in _playerManager.Sessions)
            {
                if (session.UserId != player)
                    continue;

                var msg = new CEMsgAllAchievementsData
                {
                    PlayerAchievements = new HashSet<string>(_playerAchievementsCache.GetValueOrDefault(player, new HashSet<string>())),
                    AchievementPercentages = _cachedPercentages,
                };

                _netManager.ServerSendMessage(msg, session.Channel);
            }

            return removed;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to remove achievement {achievementProtoId} from {player}: {e}");
            return false;
        }
    }
}
