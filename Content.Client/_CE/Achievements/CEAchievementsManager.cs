using Content.Shared._CE.Achievements;
using Content.Shared._CE.Achievements.Prototypes;
using Robust.Client;
using Robust.Client.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._CE.Achievements;

/// <summary>
/// Client-side manager that receives and caches achievement data from the server.
/// Registered early in IoC so the net message is known before the connection is established.
/// </summary>
public sealed partial class CEAchievementsManager : ICEAchievementsRequirementManager
{
    [Dependency] private IClientNetManager _netManager = default!;
    [Dependency] private IBaseClient _client = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    /// <summary>
    /// Achievement prototype IDs that the current player has earned.
    /// </summary>
    public HashSet<string> PlayerAchievements { get; private set; } = new();

    /// <summary>
    /// Percentage of all players who have each achievement (0–100).
    /// </summary>
    public Dictionary<string, float> AchievementPercentages { get; private set; } = new();

    /// <summary>
    /// Whether data has been received from the server at least once.
    /// </summary>
    public bool DataLoaded { get; private set; }

    /// <summary>
    /// Fired when achievement data is received from the server.
    /// </summary>
    public event Action? AchievementsUpdated;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<CEMsgAllAchievementsData>(OnAchievementsReceived);
        _client.RunLevelChanged += OnRunLevelChanged;
    }

    private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.NewLevel == ClientRunLevel.Initialize)
        {
            // Reset on disconnect.
            PlayerAchievements = new HashSet<string>();
            AchievementPercentages = new Dictionary<string, float>();
            DataLoaded = false;
        }
    }

    private void OnAchievementsReceived(CEMsgAllAchievementsData ceMsgAll)
    {
        PlayerAchievements = ceMsgAll.PlayerAchievements;
        AchievementPercentages = ceMsgAll.AchievementPercentages;
        DataLoaded = true;

        AchievementsUpdated?.Invoke();
    }

    public bool HasAchievement(NetUserId userId, ProtoId<CEAchievementPrototype> achievement)
    {
        if (_playerManager.LocalSession?.UserId != userId)
            return false;

        return PlayerAchievements.Contains(achievement);
    }
}
