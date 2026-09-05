using Content.Shared._CE.Achievements.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Achievements;

public interface ICEAchievementsRequirementManager
{
    void Initialize();

    HashSet<string> PlayerAchievements { get; }
    Dictionary<string, float> AchievementPercentages { get; }
    bool DataLoaded { get; }
    event Action? AchievementsUpdated;

    bool HasAchievement(NetUserId userId, ProtoId<CEAchievementPrototype> achievement);
}
